namespace ContentForge.Api.Llm;

/// <summary>A system + user prompt pair ready to send to an LLM.</summary>
public sealed record LlmPrompt(string System, string User);
