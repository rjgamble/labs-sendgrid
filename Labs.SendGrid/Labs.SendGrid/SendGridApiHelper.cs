using Newtonsoft.Json;

namespace Labs.SendGrid;

public class SendGridApiHelper
{
    public record ListResultMetadata([property:JsonProperty("count")] int Count);
    public record ListResult<T>(
        [property:JsonProperty("result")] List<T> Result,
        [property:JsonProperty("_metadata")] ListResultMetadata Metadata);

    public record SendGridTemplate(
        [property:JsonProperty("id")] string Id,
        [property:JsonProperty("name")] string Name,
        [property:JsonProperty("generation")] string Generation,
        [property:JsonProperty("updated_at")] string UpdatedAt);

    public record Warning([property: JsonProperty("message")] string Message);

    public record SendGridTemplateVersion(
        [property:JsonProperty("template_id")] string TemplateId,
        [property:JsonProperty("name")] string Name,
        [property:JsonProperty("subject")] string Subject,
        [property:JsonProperty("html_content")] string Content,
        [property:JsonProperty("active")] int Active = 1,
        [property:JsonProperty("id")] string? Id = null,
        [property:JsonProperty("warnings")] Warning[]? Warnings = null,
        [property:JsonProperty("test_data")] string? TestData = null);
}