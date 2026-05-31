using Core;

namespace Infrastructure.Media;

public static class MediaResolver
{
    public static bool IsLocal(SearchResultItem item)
    {
        return File.Exists(item.LocalPath);
    }
}