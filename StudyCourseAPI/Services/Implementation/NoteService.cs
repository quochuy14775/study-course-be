using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class NoteService : INoteService
{
    private readonly IRepository<LessonNote> _noteRepository;
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly ICurrentUser _currentUser;

    public NoteService(
        IRepository<LessonNote> noteRepository,
        IRepository<Lesson> lessonRepository,
        ICurrentUser currentUser)
    {
        _noteRepository = noteRepository;
        _lessonRepository = lessonRepository;
        _currentUser = currentUser;
    }

    public async Task<List<NoteResponse>> GetByLessonAsync(long lessonId)
    {
        var userId = _currentUser.GetCurrentUserId();

        return await _noteRepository.Query()
            .AsNoTracking()
            .Where(n => n.LessonId == lessonId && n.UserId == userId && !n.IsDeleted)
            .OrderBy(n => n.VideoTimestamp)
            .Select(n => new NoteResponse(n))
            .ToListAsync();
    }

    public async Task<ServiceResult<NoteResponse>> CreateAsync(long lessonId, NoteRequest model)
    {
        var lessonExists = await _lessonRepository.Query()
            .AnyAsync(l => l.Id == lessonId && !l.IsDeleted);

        if (!lessonExists)
            return ServiceResult<NoteResponse>.NotFound("Lesson not found.");

        var (valid, errors) = model.ValidateNote();
        if (!valid)
            return ServiceResult<NoteResponse>.Invalid(errors);

        var entity = model.GetNote(lessonId, _currentUser.GetCurrentUserId());

        _noteRepository.Add(entity);
        await _noteRepository.SaveChangesAsync();

        return ServiceResult<NoteResponse>.Ok(new NoteResponse(entity));
    }

    public async Task<ServiceResult<NoteResponse>> UpdateAsync(long lessonId, long noteId, NoteRequest model)
    {
        var entity = await FindOwnNoteAsync(lessonId, noteId);
        if (entity == null)
            return ServiceResult<NoteResponse>.NotFound();

        var (valid, errors) = model.ValidateNote();
        if (!valid)
            return ServiceResult<NoteResponse>.Invalid(errors);

        model.MapTo(entity);
        await _noteRepository.SaveChangesAsync();

        return ServiceResult<NoteResponse>.Ok(new NoteResponse(entity));
    }

    public async Task<bool> SoftDeleteAsync(long lessonId, long noteId)
    {
        var entity = await FindOwnNoteAsync(lessonId, noteId);
        if (entity == null) return false;

        entity.IsDeleted = true;
        await _noteRepository.SaveChangesAsync();

        return true;
    }

    /// <summary>Lấy note theo id nhưng chỉ khi nó thuộc về user đang đăng nhập.</summary>
    private Task<LessonNote?> FindOwnNoteAsync(long lessonId, long noteId)
    {
        var userId = _currentUser.GetCurrentUserId();

        return _noteRepository.Query()
            .FirstOrDefaultAsync(n =>
                n.Id == noteId &&
                n.LessonId == lessonId &&
                n.UserId == userId &&
                !n.IsDeleted);
    }
}
