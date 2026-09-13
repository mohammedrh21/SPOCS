namespace SPOCS.Application.Contracts.AI;

/// <summary>
/// Provider-agnostic interface for generating text embeddings.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Number of dimensions in the generated vectors.</summary>
    int Dimensions { get; }

    /// <summary>
    /// Generates a float vector embedding for the given text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
