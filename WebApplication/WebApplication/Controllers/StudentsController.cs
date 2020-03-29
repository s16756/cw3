using System;
using Microsoft.AspNetCore.Mvc;
using WebApplication.DAL;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
        private readonly IDbService _dbService;

        public StudentsController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet]
        public IActionResult GetStudents()
        {
            return Ok(_dbService.GetStudents());
        }
        
        [HttpGet("{indexNumber}")]
        public IActionResult GetStudent(string indexNumber)
        {
            var student = _dbService.GetStudent(indexNumber);
            if (student != null)
                return Ok(student);
            return NotFound("Nie znaleziono studenta");
        }

        [HttpPost]
        public IActionResult CreateStudent(Student student)
        {
            student.IndexNumber = $"s{new Random().Next(1, 20000)}";
            return Ok(student);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateStudent(int id, [FromBody] Student student)
        {
            return Ok("Aktualizacja zakończona");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteStudent(int id)
        {
            return Ok("Usuwanie ukończone");
        }
    }
}