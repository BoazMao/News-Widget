namespace sellthenews.Models;

public enum NewsCategory
{
    General,
    Business,
    Technology,
    Science,
    Health
}

public static class NewsCategoryExtensions
{
    public static string ToApiValue(this NewsCategory category) =>
        category.ToString().ToLowerInvariant();
}
