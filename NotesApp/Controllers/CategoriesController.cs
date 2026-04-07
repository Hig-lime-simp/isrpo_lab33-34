using Microsoft.AspNetCore.Mvc;
using NotesApp.Helpers;
using NotesApp.Models;
using NotesApp.Models.DTOs;
using NotesApp.Repositories;

namespace NotesApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repo;
    
    public CategoriesController(ICategoryRepository repo)
    {
        _repo = repo;
    }
    
    // GET /api/categories
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CategoryResponseDto>>>> GetAll()
    {
        var categories = await _repo.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<CategoryResponseDto>>.Ok(categories));
    }
    
    // GET /api/categories/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> GetById(int id)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category is null)
            return NotFound(ApiError.NotFound($"Категория с id={id} не найдена"));
        
        // Преобразуем Category в CategoryResponseDto
        var response = new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Color = category.Color,
            CreatedAt = category.CreatedAt,
            NotesCount = 0 // Здесь можно добавить подсчёт заметок при необходимости
        };
        
        return Ok(ApiResponse<CategoryResponseDto>.Ok(response));
    }
    
    // GET /api/categories/{id}/notes
    [HttpGet("{id}/notes")]
    public async Task<ActionResult<ApiResponse<object>>> GetWithNotes(int id)
    {
        var category = await _repo.GetByIdWithNotesAsync(id);
        if (category is null)
            return NotFound(ApiError.NotFound($"Категория с id={id} не найдена"));
        
        var result = new
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Color = category.Color,
            CreatedAt = category.CreatedAt,
            Notes = category.Notes.Select(n => new
            {
                n.Id,
                n.Title,
                n.IsPinned,
                n.IsArchived,
                n.Priority,
                n.CreatedAt,
                n.UpdatedAt
            })
        };
        
        return Ok(ApiResponse<object>.Ok(result));
    }
    
    // POST /api/categories
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> Create([FromBody] CreateCategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            Color = dto.Color,
            CreatedAt = DateTime.UtcNow
        };
        
        var created = await _repo.CreateAsync(category);
        
        var response = new CategoryResponseDto
        {
            Id = created.Id,
            Name = created.Name,
            Description = created.Description,
            Color = created.Color,
            CreatedAt = created.CreatedAt,
            NotesCount = 0
        };
        
        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            ApiResponse<CategoryResponseDto>.Created(response, "Категория успешно создана"));
    }
    
    // PUT /api/categories/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category is null)
            return NotFound(ApiError.NotFound($"Категория с id={id} не найдена"));
        
        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim() ?? string.Empty;
        category.Color = dto.Color;
        
        var updated = await _repo.UpdateAsync(category);
        
        var response = new CategoryResponseDto
        {
            Id = updated.Id,
            Name = updated.Name,
            Description = updated.Description,
            Color = updated.Color,
            CreatedAt = updated.CreatedAt,
            NotesCount = 0
        };
        
        return Ok(ApiResponse<CategoryResponseDto>.Ok(response, "Категория обновлена"));
    }
    
    // DELETE /api/categories/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category is null)
            return NotFound(ApiError.NotFound($"Категория с id={id} не найдена"));
        
        if (await _repo.HasNotesAsync(id))
        {
            return BadRequest(ApiError.BadRequest(
                "Невозможно удалить категорию: в ней есть заметки. " +
                "Сначала удалите или переместите заметки."));
        }
        
        await _repo.DeleteAsync(category);
        return NoContent();
    }
}