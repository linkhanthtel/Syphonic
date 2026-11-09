using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Syphonic.Data;
using Syphonic.Models;

namespace Syphonic.Areas.Admin.Pages.Lessons;

/// <summary>Lists one lesson's quiz questions for admins. URL: /Admin/Lessons/Questions/{lessonId}</summary>
public class QuestionsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public QuestionsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public Lesson Lesson { get; private set; } = null!;

    public IReadOnlyList<QuizQuestion> Questions { get; private set; } = Array.Empty<QuizQuestion>();

    public int AttemptCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int lessonId, CancellationToken cancellationToken)
    {
        var lesson = await _db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);
        if (lesson is null)
            return NotFound();

        Lesson = lesson;
        Questions = await _db.QuizQuestions.AsNoTracking()
            .Where(q => q.LessonId == lessonId)
            .OrderBy(q => q.OrderIndex).ThenBy(q => q.Id)
            .ToListAsync(cancellationToken);
        AttemptCount = await _db.QuizAttempts.CountAsync(a => a.LessonId == lessonId, cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int lessonId, int questionId, CancellationToken cancellationToken)
    {
        var question = await _db.QuizQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.LessonId == lessonId, cancellationToken);
        if (question is null)
            return NotFound();

        _db.QuizQuestions.Remove(question);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Message"] = "Question deleted.";
        return RedirectToPage(new { lessonId });
    }
}
