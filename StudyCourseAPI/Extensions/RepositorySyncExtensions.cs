using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Extensions;

public static class RepositorySyncExtensions
{
    /// <summary>
    /// Reconciles a many-to-many join table against a target set of ids: removes links no longer
    /// wanted, adds the missing ones, leaves the rest untouched. Callers build the two lookup
    /// queries themselves (they vary per relationship) and pass them in as tasks so both run
    /// concurrently instead of round-tripping one after the other.
    /// </summary>
    public static async Task SyncLinksAsync<TLink>(
        this IRepository<TLink> linkRepository,
        Task<List<TLink>> currentLinksTask,
        Task<List<long>> validTargetIdsTask,
        Func<TLink, long> otherIdSelector,
        Func<long, TLink> linkFactory)
        where TLink : class
    {
        await Task.WhenAll(currentLinksTask, validTargetIdsTask);

        var current = currentLinksTask.Result;
        var targetIds = validTargetIdsTask.Result.ToHashSet();
        var currentIds = current.Select(otherIdSelector).ToHashSet();

        foreach (var link in current)
        {
            if (!targetIds.Contains(otherIdSelector(link)))
                linkRepository.Remove(link);
        }

        foreach (var id in targetIds)
        {
            if (!currentIds.Contains(id))
                linkRepository.Add(linkFactory(id));
        }

        await linkRepository.SaveChangesAsync();
    }
}
