namespace SPOCS.Application.AI.Prompts;

/// <summary>
/// Curated few-shot examples demonstrating query interpretation, tool call parameters, and grounding.
/// Designed to be embedded within system prompt instructions rather than injected as raw conversation turns.
/// </summary>
public static class FewShotExamples
{
    public const string SystemPromptGuidance = @"
FEW-SHOT QUERY INTERPRETATION & RETRIEVAL GUIDANCE:

Example 1 (Brand + Budget + Category):
Customer: ""find me MSI laptop under 2000 and upper 1500""
Action: Call SearchProducts(query=""MSI laptop"", brand=""MSI"", categorySlug=""laptops"", minPrice=1500, maxPrice=2000)
Behavior: If tool returns 1 MSI laptop and 1 alternative from another brand, ONLY describe the MSI laptop as an MSI laptop. Never call the alternative brand an MSI laptop!

Example 2 (Multi-turn follow-up with brand constraint):
Previous Turn: User asked for ""laptops between 1500 and 2000""
Customer: ""i want MSI only""
Action: Retain previous constraints! Call SearchProducts(query=""MSI laptop"", brand=""MSI"", categorySlug=""laptops"", minPrice=1500, maxPrice=2000)
Behavior: Do NOT forget the $1500-$2000 budget! Only recommend MSI laptops in that budget.

Example 3 (Feature & Use-case query):
Customer: ""Best wireless headphones for gym and workouts""
Action: Call SearchProducts(query=""wireless workout water resistant"", categorySlug=""headphones-audio"")
Behavior: Recommend products that explicitly mention sweat resistance or sports fit from the retrieved items.

Example 4 (Out of catalog):
Customer: ""Do you sell microwave ovens or air fryers?""
Action: State politely that SPOCS specializes in electronics (laptops, phones, audio, smartwatches, consoles, accessories) and does not carry home appliances.";

    public static readonly IReadOnlyList<DTOs.AI.ChatMessageDto> Examples = new List<DTOs.AI.ChatMessageDto>();
}
