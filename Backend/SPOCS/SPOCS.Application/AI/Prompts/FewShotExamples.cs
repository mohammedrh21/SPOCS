using SPOCS.Application.DTOs.AI;

namespace SPOCS.Application.AI.Prompts;

/// <summary>
/// Curated few-shot conversational examples providing guidance on response style, grounding, and handling out-of-catalog inquiries.
/// </summary>
public static class FewShotExamples
{
    public static readonly IReadOnlyList<ChatMessageDto> Examples = new List<ChatMessageDto>
    {
        // Example 1: Budget & programming laptop request
        new ChatMessageDto
        {
            Role = "user",
            Content = "I need a budget laptop under $800 for programming and university."
        },
        new ChatMessageDto
        {
            Role = "assistant",
            Content = "For programming and university coursework under $800, I recommend:\n\n" +
                      "• **Acer Aspire 5 15.6\" FHD Laptop** — **$649.00**\n" +
                      "  - **Processor:** Intel Core i5-1335U (10-core, up to 4.6 GHz)\n" +
                      "  - **Memory & Storage:** 16GB DDR4 RAM, 512GB NVMe SSD\n" +
                      "  - **Display:** 15.6\" Full HD IPS with Acer BlueLightShield\n" +
                      "  - **Why it fits:** It easily handles code compilation, multi-tab web research, and daily student tasks well within your $800 budget.\n\n" +
                      "Would you like to know more about its battery life or ports?"
        },

        // Example 2: Wireless gaming headphones
        new ChatMessageDto
        {
            Role = "user",
            Content = "Which headphones are best for wireless gaming?"
        },
        new ChatMessageDto
        {
            Role = "assistant",
            Content = "For wireless gaming, the top choice in our catalog is:\n\n" +
                      "• **SteelSeries Arctis Nova Pro Wireless Gaming Headset** — **$349.99**\n" +
                      "  - **Connectivity:** Ultra-low latency 2.4GHz wireless plus concurrent Bluetooth\n" +
                      "  - **Audio:** 40mm Neodymium drivers with 360° Spatial Audio & parametric EQ\n" +
                      "  - **Microphone:** ClearCast Gen 2 bidirectional noise-canceling mic\n" +
                      "  - **Key Feature:** Dual hot-swappable batteries so you never have to pause a game to charge.\n\n" +
                      "Let me know if you'd like to compare it with any of our other audio headsets!"
        },

        // Example 3: Out-of-catalog inquiry
        new ChatMessageDto
        {
            Role = "user",
            Content = "Do you sell mechanical keyboards or gaming monitors?"
        },
        new ChatMessageDto
        {
            Role = "assistant",
            Content = "We currently do not carry standalone mechanical keyboards or gaming monitors in our catalog.\n\n" +
                      "Our current selection focuses on **Laptops**, **Smartphones**, **Headphones & Audio**, and **Smartwatches**. If you are looking for a gaming device with a built-in high refresh screen and keyboard, we do carry high-performance gaming laptops such as the **ASUS ROG Zephyrus G16** (240Hz OLED)."
        }
    };
}
