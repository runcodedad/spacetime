using System.Buffers;
using MerkleTree.Cache;
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
        string? cacheFilePath = null,
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

        // Split progress: 50% for scanning, 50% for Merkle proof generation
        IProgress<double>? scanProgress = null;
        IProgress<double>? merkleProgress = null;
        
        if (progress != null)
        {
            scanProgress = new Progress<double>(p => progress.Report(p * 0.5));
            merkleProgress = new Progress<double>(p => progress.Report(50 + (p * 0.5)));
        }

        // Scan the plot to find the best score (0-50%)
        var bestResult = await ScanPlotAsync(
            plotLoader,
            challenge,
            strategy,
            scanProgress,
            cancellationToken);

        if (bestResult == null)
        {
            return null;
        }

        // Generate Merkle proof for the winning leaf (50-100%)
        var merkleProof = await GenerateMerkleProofAsync(
            plotLoader,
            bestResult.LeafIndex,
            cacheFilePath,
            merkleProgress,
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
        IReadOnlyList<ProofGenerationOptions> options,
        byte[] challenge,
        IScanningStrategy strategy,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(strategy);

        if (options.Count == 0 || options.Any(o => o.PlotLoader == null))
        {
            throw new ArgumentException("Options must provide at least one plot loader", nameof(options));
        }

        if (challenge.Length != 32)
        {
            throw new ArgumentException("Challenge must be 32 bytes", nameof(challenge));
        }

        // Track best proof as we go instead of collecting all in memory
        Proof? bestProof = null;
        var bestProofLock = new object();

        // Generate proofs from all plots in parallel
        var tasks = options.Select(async options => 
        {
            try
            {
                var proof = await GenerateProofAsync(options.PlotLoader, challenge, strategy, options.CacheFilePath, null, cancellationToken);
                if (proof != null)
                {
                    // Update best proof if this one is better
                    lock (bestProofLock)
                    {
                        if (bestProof == null || CompareScores(proof.Score, bestProof.Score) < 0)
                        {
                            bestProof = proof;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If one plot fails, continue with others
            }
        }).ToArray();

        await Task.WhenAll(tasks);

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

        var indicesToScan = strategy.GetIndicesToScan(plotLoader.LeafCount);
        var totalToScan = strategy.GetScanCount(plotLoader.LeafCount);
        long scanned = 0;

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
                // Store leaf directly (already a byte array)
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
        string? cacheFilePath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        // Use PlotLoader's optimized sequential read (no seeking between leaves)
        var leavesStream = plotLoader.ReadAllLeavesAsync(progress, cancellationToken);

        CacheData? cacheData = null;
        if (cacheFilePath != null && File.Exists(cacheFilePath))
        {
            // TODO: should this be async?
            cacheData = CacheFileManager.LoadCache(cacheFilePath);
        }

        // Use MerkleTreeStream to generate proof
        // Note: MerkleTreeStream doesn't support progress reporting yet
        var merkleTreeStream = new MerkleTreeStream(_hashFunction);
        var merkleProof = await merkleTreeStream.GenerateProofAsync(
            leavesStream,
            leafIndex,
            plotLoader.LeafCount,
            cache: cacheData,
            cancellationToken);

        // Report completion of this phase
        progress?.Report(100);

        return merkleProof;
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
    private static int CompareScores(ReadOnlySpan<byte> score1, ReadOnlySpan<byte> score2)
    {
        for (var i = 0; i < score1.Length && i < score2.Length; i++)
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
    /// Compares two scores (byte array version for compatibility).
    /// </summary>
    private static int CompareScores(byte[] score1, byte[] score2)
    {
        return CompareScores(score1.AsSpan(), score2.AsSpan());
    }

    /// <summary>
    /// Internal result of scanning a plot.
    /// </summary>
    private sealed record ScanResult(byte[] LeafValue, long LeafIndex, byte[] Score);
}

public sealed record ProofGenerationOptions(PlotLoader PlotLoader, string? CacheFilePath);
