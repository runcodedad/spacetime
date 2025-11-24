using MerkleTree.Core;
using MerkleTree.Hashing;
using MerkleTree.Proofs;

namespace Spacetime.Plotting;

/// <summary>
/// Generates cryptographic proofs from plot files by scanning for the best score.
/// </summary>
/// <remarks>
/// The proof generator:
/// 1. Scans a plot using a specified strategy (full or sampling)
/// 2. Computes score = H(challenge || leaf) for each scanned leaf
/// 3. Tracks the lowest score found
/// 4. Generates a Merkle proof for the winning leaf
/// 5. Returns a complete Proof object for blockchain consensus
/// </remarks>
public sealed class ProofGenerator
{
    private readonly IHashFunction _hashFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofGenerator"/> class.
    /// </summary>
    /// <param name="hashFunction">Hash function for score computation and Merkle proofs</param>
    public ProofGenerator(IHashFunction hashFunction)
    {
        ArgumentNullException.ThrowIfNull(hashFunction);
        _hashFunction = hashFunction;
    }

    /// <summary>
    /// Generates a proof from a plot file for the given challenge.
    /// </summary>
    /// <param name="plotLoader">The loaded plot file</param>
    /// <param name="challenge">The 32-byte challenge to generate proof for</param>
    /// <param name="strategy">The scanning strategy to use</param>
    /// <param name="progress">Optional progress reporter (reports percentage 0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A proof containing the best score found, or null if no valid proof could be generated</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when challenge is not 32 bytes</exception>
    public async Task<Proof?> GenerateProofAsync(
        PlotLoader plotLoader,
        byte[] challenge,
        IScanningStrategy strategy,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plotLoader);
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(strategy);

        if (challenge.Length != 32)
        {
            throw new ArgumentException("Challenge must be 32 bytes", nameof(challenge));
        }

        // Scan the plot to find the best score
        var bestResult = await ScanPlotAsync(
            plotLoader,
            challenge,
            strategy,
            progress,
            cancellationToken);

        if (bestResult == null)
        {
            return null;
        }

        // Generate Merkle proof for the winning leaf
        var merkleProof = await GenerateMerkleProofAsync(
            plotLoader,
            bestResult.LeafIndex,
            cancellationToken);

        // Build the complete proof object
        var proof = new Proof(
            leafValue: bestResult.LeafValue,
            leafIndex: bestResult.LeafIndex,
            siblingHashes: merkleProof.SiblingHashes,
            orientationBits: merkleProof.SiblingIsRight,
            merkleRoot: plotLoader.MerkleRoot.ToArray(),
            challenge: challenge,
            score: bestResult.Score);

        return proof;
    }

    /// <summary>
    /// Generates proofs from multiple plots in parallel and returns the best one.
    /// </summary>
    /// <param name="plotLoaders">Collection of loaded plot files</param>
    /// <param name="challenge">The 32-byte challenge to generate proof for</param>
    /// <param name="strategy">The scanning strategy to use for each plot</param>
    /// <param name="progress">Optional progress reporter (reports percentage 0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The best proof found across all plots, or null if no valid proof could be generated</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when challenge is not 32 bytes or plotLoaders is empty</exception>
    public async Task<Proof?> GenerateProofFromMultiplePlotsAsync(
        IReadOnlyList<PlotLoader> plotLoaders,
        byte[] challenge,
        IScanningStrategy strategy,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plotLoaders);
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(strategy);

        if (plotLoaders.Count == 0)
        {
            throw new ArgumentException("Must provide at least one plot loader", nameof(plotLoaders));
        }

        if (challenge.Length != 32)
        {
            throw new ArgumentException("Challenge must be 32 bytes", nameof(challenge));
        }

        // Generate proofs from all plots in parallel
        var tasks = plotLoaders.Select(async loader =>
        {
            try
            {
                return await GenerateProofAsync(loader, challenge, strategy, null, cancellationToken);
            }
            catch (Exception)
            {
                // If one plot fails, continue with others
                return null;
            }
        }).ToArray();

        var proofs = await Task.WhenAll(tasks);

        // Filter out nulls and find the proof with the lowest score
        var validProofs = proofs.Where(p => p != null).ToArray();
        
        if (validProofs.Length == 0)
        {
            return null;
        }

        // Compare scores (lower is better)
        var bestProof = validProofs.OrderBy(p => p!.Score, new ByteArrayComparer()).First();

        progress?.Report(100);

        return bestProof;
    }

    /// <summary>
    /// Scans a plot to find the leaf with the best (lowest) score.
    /// </summary>
    private async Task<ScanResult?> ScanPlotAsync(
        PlotLoader plotLoader,
        byte[] challenge,
        IScanningStrategy strategy,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        byte[]? bestLeaf = null;
        long bestLeafIndex = -1;
        byte[]? bestScore = null;

        var indicesToScan = strategy.GetIndicesToScan(plotLoader.LeafCount).ToArray();
        var totalToScan = indicesToScan.Length;
        var scanned = 0;

        foreach (var leafIndex in indicesToScan)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Read leaf from plot
            var leaf = await plotLoader.ReadLeafAsync(leafIndex, cancellationToken);

            // Compute score = H(challenge || leaf)
            var score = ComputeScore(challenge, leaf);

            // Track best score (lowest value wins)
            if (bestScore == null || CompareScores(score, bestScore) < 0)
            {
                bestScore = score;
                bestLeaf = leaf;
                bestLeafIndex = leafIndex;
            }

            scanned++;
            if (progress != null && scanned % 100 == 0)
            {
                var percentage = (double)scanned / totalToScan * 100.0;
                progress.Report(percentage);
            }
        }

        progress?.Report(100);

        if (bestLeaf == null)
        {
            return null;
        }

        return new ScanResult(bestLeaf, bestLeafIndex, bestScore!);
    }

    /// <summary>
    /// Generates a Merkle proof for a specific leaf in the plot.
    /// </summary>
    private async Task<MerkleProof> GenerateMerkleProofAsync(
        PlotLoader plotLoader,
        long leafIndex,
        CancellationToken cancellationToken)
    {
        // Create a streaming async enumerable of all leaves
        var leavesStream = ReadAllLeavesAsync(plotLoader, cancellationToken);

        // Use MerkleTreeStream to generate proof
        var merkleTreeStream = new MerkleTreeStream(_hashFunction);
        var merkleProof = await merkleTreeStream.GenerateProofAsync(
            leavesStream,
            leafIndex,
            plotLoader.LeafCount,
            cache: null, // TODO: Support cache if available
            cancellationToken);

        return merkleProof;
    }

    /// <summary>
    /// Reads all leaves from a plot as an async enumerable.
    /// </summary>
    private static async IAsyncEnumerable<byte[]> ReadAllLeavesAsync(
        PlotLoader plotLoader,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (long i = 0; i < plotLoader.LeafCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var leaf = await plotLoader.ReadLeafAsync(i, cancellationToken);
            yield return leaf;
        }
    }

    /// <summary>
    /// Computes the score for a leaf given a challenge.
    /// Score = H(challenge || leaf)
    /// </summary>
    private byte[] ComputeScore(byte[] challenge, byte[] leaf)
    {
        var input = new byte[challenge.Length + leaf.Length];
        challenge.CopyTo(input.AsSpan());
        leaf.CopyTo(input.AsSpan(challenge.Length));

        return _hashFunction.ComputeHash(input);
    }

    /// <summary>
    /// Compares two scores. Returns negative if score1 is better (lower).
    /// </summary>
    private static int CompareScores(byte[] score1, byte[] score2)
    {
        for (var i = 0; i < score1.Length; i++)
        {
            var diff = score1[i] - score2[i];
            if (diff != 0)
            {
                return diff;
            }
        }
        return 0;
    }

    /// <summary>
    /// Internal result of scanning a plot.
    /// </summary>
    private sealed record ScanResult(byte[] LeafValue, long LeafIndex, byte[] Score);

    /// <summary>
    /// Comparer for byte arrays (used for finding lowest score).
    /// </summary>
    private sealed class ByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[]? x, byte[]? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            return CompareScores(x, y);
        }
    }
}
