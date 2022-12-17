﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Aula1.Data;
using Aula1.Models;
using Aula1.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.IO;

namespace Aula1.Controllers
{
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CursosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cursos
        public async Task<IActionResult> Index(bool? disponivel)
        {
            ViewData["CategoriaId"] = new SelectList(_context.Categoria.ToList(), "Id", "Nome");

            if (disponivel != null)
            {
                if (disponivel == true)
                    ViewData["Title"] = "Lista de cursos Activos";
                else
                    ViewData["Title"] = "Lista de cursos Inativos";
                return View(await _context.Cursos.Include("Categoria").Where(c => c.Disponivel == disponivel).ToListAsync());
            }

            ViewData["Title"] = "Lista de cursos";
            return View(await _context.Cursos.Include("Categoria").ToListAsync());
        }
        /*
        [HttpPost]
        public async Task<IActionResult> Index(string textoAPesquisar )
        {
            return View(await _context.Cursos.Where(
                c=>c.Nome.Contains(textoAPesquisar) 
                || c.Descricao.Contains(textoAPesquisar)
                ).ToListAsync());
        }
        */
        // GET: Cursos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Cursos == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos.Include("Categoria")
                .FirstOrDefaultAsync(m => m.Id == id);
            if (curso == null)
            {
                return NotFound();
            }

            return View(curso);
        }

        [HttpPost]
        public async Task<IActionResult> Search([Bind("TextoAPesquisar")]
            PesquisaCursoViewModel pesquisaCurso, int CategoriaId)
        {
            if (string.IsNullOrWhiteSpace(pesquisaCurso.TextoAPesquisar))
            {
                pesquisaCurso.ListaDeCursos = await _context.Cursos.Include("Categoria").
                    Where(c => c.CategoriaId == CategoriaId).ToListAsync();
            }
            else
            {

                pesquisaCurso.ListaDeCursos = await _context.Cursos.Include("Categoria").Where(
                    c => c.Nome.Contains(pesquisaCurso.TextoAPesquisar)
                    || c.Descricao.Contains(pesquisaCurso.TextoAPesquisar)
                    || c.CategoriaId == CategoriaId
                    ).ToListAsync();

            }
            pesquisaCurso.NumResultados = pesquisaCurso.ListaDeCursos.Count;

            ViewData["Title"] = "Os nossos cursos";

            return View(pesquisaCurso);
        }

        // GET: Cursos/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CategoriaId"] = new SelectList(_context.Categoria.ToList(), "Id", "Nome");
            return View();
        }

        // POST: Cursos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nome,Disponivel,CategoriaId,Descricao,DescricaoResumida,Requisitos,IdadeMinima,Preco,EmDestaque")] Curso curso)
        {
            ViewData["CategoriaId"] = new SelectList(_context.Categoria.ToList(), "Id", "Nome");

            //temporário
            ModelState.Remove(nameof(curso.Categoria));
            if (ModelState.IsValid)
            {
                _context.Add(curso);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(curso);
        }

        // GET: Cursos/Edit/5

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            ViewData["CategoriaId"] = new SelectList(_context.Categoria.ToList(), "Id", "Nome");

            if (id == null || _context.Cursos == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null)
            {
                return NotFound();
            }

            ViewData["CategoriaId"] = new SelectList(_context.Categoria.ToList(), "Id", "Nome");

            var coursePath = Path.Combine(Directory.GetCurrentDirectory(), ("wwwroot/img/cursos/" + id.ToString()));
            if (!Directory.Exists(coursePath))
                Directory.CreateDirectory(coursePath);
            //LINK SYNTAX
            var files = from file in
                            Directory.EnumerateFiles(coursePath)
                        select string.Format(
                            "/img/cursos//{0}/{1}",
                            id,
                            Path.GetFileName(file));

            ViewData["Ficheiros"] = files; //lista de strings para a vista
            //ViewBag.Ficheiros = files;

            return View(curso);
        }

        // POST: Cursos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Disponivel,CategoriaId,Descricao,DescricaoResumida,Requisitos,IdadeMinima,Preco,EmDestaque")] Curso curso,
            [FromForm] List<IFormFile> ficheiros)
        {
            ViewData["CategoriaId"] = new SelectList(_context.Categoria.ToList(), "Id", "Nome");

            if (id != curso.Id)
            {
                return NotFound();
            }

            //temporário
            ModelState.Remove(nameof(curso.Categoria));
            if (ModelState.IsValid)
            {
                try
                {

                    _context.Update(curso);
                    await _context.SaveChangesAsync();

                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/cursos/");
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    // Dir relativo aos ficheiros do curso
                    path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/cursos/" + id.ToString());
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    foreach (var formFile in ficheiros)
                    {
                        if (formFile.Length > 0)
                        {
                            var filePath = Path.Combine(path, Guid.NewGuid().ToString() + Path.GetExtension(formFile.FileName));
                            while (System.IO.File.Exists(filePath))
                            {
                                filePath = Path.Combine(path, Guid.NewGuid().ToString() + Path.GetExtension(formFile.FileName));
                            }
                            using (var stream = System.IO.File.Create(filePath))
                            {
                                await formFile.CopyToAsync(stream);
                            }
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CursoExists(curso.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(curso);
        }

        public async Task<IActionResult> deleteImage(int id, string image)
        {
            if (id == null || _context.Cursos == null)
                return NotFound();
            var curso = await _context.Cursos.FirstOrDefaultAsync(m => m.Id == id);
            if (curso == null)
                return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), ("wwwroot/" + image));

            System.IO.File.Delete(filePath);
            return RedirectToAction("Edit", new { Id = id });
        }

        // GET: Cursos/Delete/5

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Cursos == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos.Include("Categoria")
                .FirstOrDefaultAsync(m => m.Id == id);
            if (curso == null)
            {
                return NotFound();
            }

            return View(curso);
        }

        // POST: Cursos/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Cursos == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Cursos'  is null.");
            }
            var curso = await _context.Cursos.FindAsync(id);
            if (curso != null)
            {
                _context.Cursos.Remove(curso);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CursoExists(int id)
        {
          return _context.Cursos.Any(e => e.Id == id);
        }
    }
}
