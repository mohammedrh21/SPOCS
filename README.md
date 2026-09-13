# SPOCS — AI Shopping Assistant

> **A simple E-Commerce project focused on demonstrating how AI can transform the traditional online shopping experience.**

SPOCS is a simple E-Commerce application created primarily to demonstrate **AI integration in an online shopping experience**.

Instead of building a large commercial E-Commerce platform, SPOCS keeps the traditional E-Commerce functionality intentionally simple and focuses on adding an intelligent **AI Shopping Assistant** powered by modern AI techniques.

The project demonstrates how customers can interact with products using **natural language** instead of relying only on traditional search and filtering.

---

## ✨ Project Idea

Traditional E-Commerce usually requires customers to:

1. Browse categories.
2. Search using keywords.
3. Apply filters.
4. Open product pages.
5. Compare products manually.

SPOCS introduces an AI-powered shopping assistant that allows customers to simply describe what they need.

For example:

> "I need a cheap laptop for programming."

Instead of requiring the customer to know the exact product keywords, the AI can understand the customer's intent, search the available products semantically, apply relevant constraints, and return suitable products.

### The core idea

```text
Traditional E-Commerce
        +
     Product Data
        +
     Embeddings
        +
 Semantic Product Search
        +
        LLM
        +
 Few-Shot Prompting
        +
        RAG
        +
    AI Agent
        ↓
 AI Shopping Assistant
```

---

## 🤖 AI Features

AI is the **main focus of SPOCS**.

### Large Language Model

An LLM is used to understand customer questions and generate natural responses.

The assistant can help customers:

* Find products
* Understand product features
* Ask questions about products
* Compare relevant products
* Search using natural language
* Find products based on requirements

---

### 🔎 Semantic Product Search

SPOCS uses **embeddings** to understand the semantic meaning of product information and customer queries.

Instead of matching only exact keywords, semantic search can identify products that are conceptually related to the customer's request.

For example:

```text
Customer:
"I need headphones for gaming"

        ↓

Semantic Search

        ↓

Products related to:
Gaming + Headphones + Relevant Features
```

---

### 🧠 Retrieval-Augmented Generation (RAG)

The AI assistant uses a basic RAG workflow to ground its responses in actual product data.

```text
Customer Question
        ↓
Query Understanding
        ↓
Query Embedding
        ↓
Semantic Search
        ↓
Relevant Products
        ↓
Structured Filtering
        ↓
Context
        ↓
LLM
        ↓
Final Answer
```

The retrieved product information is treated as the **source of truth**.

The AI should not invent product names, prices, specifications, features, or availability.

---

### 🎯 Few-Shot Prompting

Few-shot examples are used to help the LLM understand how customer requests should be interpreted.

Example:

```text
User:
"I need a cheap phone with a good camera."

Expected interpretation:
Intent: ProductSearch
Category: Smartphones
Preferences:
- Low price
- Good camera
```

This helps the AI consistently understand common shopping requirements.

---

### 🛒 AI Shopping Agent

SPOCS goes beyond a generic chatbot by treating the AI as a simple **shopping agent**.

Depending on the customer's request, the agent can determine when product retrieval is necessary.

Possible capabilities include:

```text
SearchProducts
GetProductById
GetProductDetails
FindSimilarProducts
```

The goal is not to create a complex autonomous agent, but to demonstrate how AI can interact intelligently with an E-Commerce product catalog.

---

## 🛍️ E-Commerce Features

The E-Commerce functionality is intentionally kept simple.

### Customer Features

* Customer registration
* Customer login
* Browse categories
* Browse products
* View product details
* View product variants
* Search products
* Basic product filtering
* Add products/variants to cart
* View cart
* Create a simple order
* View customer orders
* AI Shopping Assistant

---

## 🚫 Project Scope

SPOCS is **not intended to be a complete commercial E-Commerce platform**.

The following are intentionally excluded:

* Admin Control Panel
* Product CRUD
* Category CRUD
* Variant CRUD
* Inventory management
* Vendor management
* Store management
* Payment gateway integration
* Stripe integration
* Complex order management
* Shipping management
* Coupon system
* Wishlist
* Product reviews
* Recommendation engine
* Notifications

The purpose of keeping the scope small is to allow the project to focus on the **AI implementation**.

---

## 🌱 Seeded Product Data

Products and related E-Commerce data are seeded into the database.

The application does not require an Admin UI for managing products.

The seed data includes:

* Categories
* Products
* Product variants
* Variant types
* Variant options
* Product images
* Test users where useful

Products contain meaningful information such as:

* Product names
* Categories
* Descriptions
* Features
* Specifications
* Prices
* Variant information

The dataset should contain enough realistic products to make semantic search meaningful while remaining small enough for demonstration purposes.

---

## 🧩 Product Information & Embeddings

The AI system primarily works with product-related information.

A searchable product representation can contain:

```text
Product Name
Category
Description
Features
Specifications
Relevant Variant Information
```

This information is used to generate embeddings for semantic search.

Structured information such as:

```text
Price
Quantity
Stock Status
Product ID
```

remains available as structured database information rather than relying entirely on embeddings.

This allows SPOCS to combine:

```text
Semantic Search
        +
Structured Filtering
```

For example:

```text
User:
"Find me a gaming laptop under $1000."

Semantic Search:
Gaming laptops

        +

Structured Filter:
Price <= $1000

        ↓

Relevant products matching both requirements
```

---

## 🏗️ Architecture

The project follows a clean separation between the normal E-Commerce functionality and the AI functionality.

### Backend

The backend follows a maintainable architecture based on:

* ASP.NET Core Web API
* C#
* Entity Framework Core
* SQL Server
* JWT Authentication
* REST API
* Clean Architecture principles
* SOLID principles
* Dependency Injection
* DTOs
* Async/await

### AI Separation

AI-related functionality is separated from normal E-Commerce functionality.

Conceptually:

```text
Application
│
├── Products
├── Categories
├── Cart
├── Orders
│
└── AI
    ├── Chat
    ├── Embeddings
    ├── Retrieval
    ├── Prompts
    └── Tools
```

AI providers should be accessed through abstractions where practical, such as:

```text
ILLMService
IEmbeddingService
IVectorSearchService
IAiChatService
```

This keeps the AI layer maintainable and makes it easier to change providers when necessary.

---

## 🎨 Frontend

The frontend is built using **React JS** and follows a **Feature-Based Architecture**.

The UI uses:

* React JS
* React Hooks
* Tailwind CSS
* shadcn/ui

The design focuses on providing a modern, soft, professional E-Commerce experience without allowing the UI work to overshadow the AI functionality.

The main frontend areas include:

```text
Home
Categories
Products
Product Details
Cart
Orders
Authentication
AI Shopping Assistant
```

---

## 🔄 AI Shopping Flow

A typical AI shopping interaction looks like this:

```text
Customer
   │
   │ "I need a laptop for university under $1000"
   ↓
AI Shopping Assistant
   │
   ↓
Understand Customer Intent
   │
   ↓
Generate Search Representation
   │
   ↓
Semantic Product Search
   │
   ↓
Retrieve Relevant Products
   │
   ↓
Apply Structured Requirements
   │
   ↓
Build Context
   │
   ↓
LLM
   │
   ↓
Helpful Grounded Response
   │
   ↓
Customer
```

---

## 🧪 Example AI Queries

SPOCS is designed to handle natural-language shopping requests such as:

```text
"I need a cheap laptop for programming."

"Which phones have a good camera?"

"Show me wireless headphones for gaming."

"What is the difference between these two phones?"

"I need a laptop under $1000 for university."

"Find me something good for gaming."

"I need a phone with a large screen and good battery life."
```

The AI should use the available product data to provide relevant and grounded answers.

---

## 🛡️ Hallucination Control

A major goal of the AI implementation is to keep responses grounded in real application data.

The assistant should **not invent**:

* Product names
* Prices
* Specifications
* Features
* Availability
* Discounts
* Stock quantities

If no suitable product information is available, the assistant should clearly communicate that.

For example:

> "I couldn't find a product matching those requirements in the available products."

This is preferable to generating a fictional product.

---

## 🚀 Development Phases

### Phase 1 — Basic E-Commerce

* Create the solution
* Create the database
* Create Product/Category/Variant entities
* Create migrations
* Seed product data
* Implement product APIs
* Implement category APIs
* Implement customer authentication
* Implement cart
* Implement simple orders

### Phase 2 — Embeddings

* Define searchable product representation
* Generate product embeddings
* Store embeddings
* Implement vector search
* Test semantic product search

### Phase 3 — LLM

* Integrate an LLM provider
* Create system prompts
* Create few-shot examples
* Implement product-related question answering
* Ground responses in retrieved product data

### Phase 4 — AI Agent

* Define useful AI tools
* Implement product retrieval
* Connect the LLM with tools
* Add semantic search
* Combine retrieval with structured filtering
* Implement RAG
* Test different customer scenarios

### Phase 5 — Demonstration & Improvement

Test scenarios including:

* Product discovery
* Natural-language search
* Price requirements
* Feature requirements
* Product comparison
* No matching products
* Ambiguous questions
* Unrelated questions
* Hallucination prevention

---

## 🎯 Project Goal

SPOCS is not measured by how many E-Commerce features it contains.

The primary goal is to demonstrate:

> **How AI can make a simple E-Commerce application more intelligent and easier to use.**

The project combines:

**LLM + Embeddings + Semantic Search + Few-Shot Prompting + RAG + AI Agent**

to create a practical **AI Shopping Assistant** on top of a simple E-Commerce experience.

---

## 📌 Project Philosophy

> **Keep the E-Commerce simple. Make the AI meaningful.**

When deciding whether to add a feature, ask:

```text
Does this improve the basic E-Commerce experience
or help demonstrate the AI functionality?
```

If neither applies, it should not be implemented.

The project should remain:

* Simple
* Understandable
* Demonstrable
* Maintainable
* AI-focused
