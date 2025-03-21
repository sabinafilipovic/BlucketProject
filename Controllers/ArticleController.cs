namespace Blocket.Controllers;

using Blocket.Models;
using Microsoft.AspNetCore.Mvc;
using Blocket.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class ArticleController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var articles = await context.Articles.OrderByDescending(a => a.Published).ToListAsync();

        var vm = new ArticleIndexVm { Articles = articles };
        return View(vm);
    }

    public IActionResult Create()
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
        {
            return Forbid();
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(ArticleCreateVm vm)
    {
        vm.Article.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (vm.Article.UserId == null || !IsAuthorized(vm.Article.UserId))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            if (vm.Article.Published.HasValue)
            {
                vm.Article.Published = vm.Article.Published.Value.ToUniversalTime();
            }

            context.Add(vm.Article);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        Article? article = await context.Articles.FirstOrDefaultAsync(a => a.Id == id);

        // "article.UserId == null" prevents a possibly nullable warning. Theoretically UserId Should never be null.
        if (article == null || article.UserId == null)
        {
            return NotFound();
        }

        if (!IsAuthorized(article.UserId))
        {
            return Forbid();
        }

        var vm = new ArticleEditVm
        {
            Id = article.Id,
            Name = article.Name,
            Description = article.Description,
            Price = article.Price,
            ImageUrl = article.ImageUrl
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ArticleEditVm vm)
    {
        // var article = vm.Article;

        if (ModelState.IsValid)
        {
            Article? article = await context.Articles.FirstOrDefaultAsync(a => a.Id == vm.Id);

            // "article.UserId == null" prevents a possibly nullable warning. Theoretically UserId Should never be null.
            if (article == null || article.UserId == null)
            {
                return NotFound();
            }


            if (!IsAuthorized(article.UserId))
            {
                return Forbid();
            }

            // Change article values:
            article.Name = vm.Name;
            article.Description = vm.Description;
            article.Price = vm.Price;
            article.ImageUrl = vm.ImageUrl;

            context.Update(article);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    public async Task<IActionResult> Delete(Article article, string previous)
    {
        var _article = await context.Articles.FirstOrDefaultAsync(a => a.Id == article.Id);

        if (_article == null)
        {
            return NotFound();
        }

        var vm = new ArticleDeleteVm
        {
            Article = _article,
            Previous = previous
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string previous)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var article = await context.Articles.FirstOrDefaultAsync(a => a.Id == id);

        if (article == null)
        {
            return NotFound();
        }

        if (!IsAuthorized(article))
        {
            // If the user is not authorized...
            return Forbid();
        }

        context.Remove(article);
        await context.SaveChangesAsync();

        if (previous == "home")
        {
            return RedirectToAction(nameof(Index), "Home");
        }

        return RedirectToAction(nameof(Index));
    }


    private bool IsAuthorized(Article article)
    {
        var isAdmin = User.IsInRole(RoleConstants.Administrator);
        var isUser = User.FindFirstValue(ClaimTypes.NameIdentifier) == article.UserId;

        if (isAdmin || isUser)
        {
            return true;
        }

        return false;
    }

    private bool IsAuthorized(string articleUserId)
    {
        var isAdmin = User.IsInRole(RoleConstants.Administrator);
        var isUser = User.FindFirstValue(ClaimTypes.NameIdentifier) == articleUserId;

        if (isAdmin || isUser)
        {
            return true;
        }

        return false;
    }
}
