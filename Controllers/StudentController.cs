using BTL.Middleware;
using BTL.Models;
using BTL.Request;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Bson;
using System.Globalization;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.IdentityModel.Tokens;

namespace BTL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [CustomAuthorization("A_Very_Secure_Secret_Key_With_32_Characters")]
    public class StudentController : ControllerBase
    {
        private readonly AdsMongoDbContext _dbContext;

        public StudentController(AdsMongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> AddStudent([FromForm] AddStudentRequest request, IFormFile? avatar)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || !Regex.IsMatch(request.FirstName, @"^[a-zA-ZÀ-ỹà-ỹ\s\d]+$"))
            {
                return BadRequest(new { code = 0, message = "Họ tên đệm học sinh không đúng định dạng" });
            }

            if (string.IsNullOrWhiteSpace(request.LastName) || !Regex.IsMatch(request.LastName, @"^[a-zA-ZÀ-ỹà-ỹ\d]+$"))
            {
                return BadRequest(new { code = 0, message = "Tên học sinh không đúng định dạng" });
            }

            if (request.Gender.HasValue && request.Gender != 1 && request.Gender != 2)
            {
                return BadRequest(new { code = 0, message = "Giá trị giới tính không hợp lệ" });
            }


            if (!string.IsNullOrWhiteSpace(request.DayOfBirth))
            {

                if (!Regex.IsMatch(request.DayOfBirth, @"^\d{2}/\d{2}/\d{4}$"))
                {
                    return BadRequest(new { code = 0, message = "Ngày sinh không đúng định dạng dd/MM/yyyy" });
                }

                if (!DateTime.TryParseExact(request.DayOfBirth, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    return BadRequest(new { code = 0, message = "Ngày sinh không đúng định dạng dd/MM/yyyy" });
                }
            }


            if (avatar != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(avatar.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { code = 0, message = "Ảnh chỉ chấp nhập file .jpg, .jpeg, .png" });
                }
            }

            try
            {
                var existingClass = await _dbContext.classes.Where(c => c.CLassId == request.ClassId).FirstOrDefaultAsync();
                if (existingClass == null)
                {
                    return BadRequest(new { code = 0, message = "Thông tin lớp học không chính xác" });
                }

                string avatarPath = "";
                if (avatar != null)
                {
                    string uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

                    if (!Directory.Exists(uploadDirectory))
                    {
                        Directory.CreateDirectory(uploadDirectory);
                    }

                    string fileExtension = Path.GetExtension(avatar.FileName);

                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(avatar.FileName);

                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                    string newFileName = $"{fileNameWithoutExtension}_{timestamp}{fileExtension}";

                    avatarPath = Path.Combine("Uploads/", newFileName);

                    using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), avatarPath), FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }
                }

                var studentId = Guid.NewGuid();
                var student = new Student
                {
                    StudentId = studentId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    ClassId = request.ClassId,
                    Gender = request.Gender ?? 1,
                    DayOfBirth = request.DayOfBirth ?? "",
                    Avatar = avatarPath
                };

                await _dbContext.students.Insert(student);

                var dataStudent = await _dbContext.students.Where(a => a.StudentId == studentId).FirstOrDefaultAsync();

                return Ok(new { code = 1, message = "Tạo học sinh thành công", data = dataStudent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 0, message = "Tạo học sinh thất bại", error = ex.Message });
            }
        }

        [HttpGet("GetStudents")]
        public async Task<IActionResult> GetStudents([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] Guid? classId = null)
        {
            try
            {
                var skip = (page - 1) * pageSize;

                var query = _dbContext.students.Where(student => student.IsDeleted == 0);

                if (classId.HasValue)
                {
                    query = query.Where(student => student.ClassId == classId.Value);
                }

                var studentList = query
                .Skip(skip)
                .Take(pageSize)
                .ToList();
               
                studentList = studentList
                    .OrderBy(student => student.LastName, StringComparer.InvariantCultureIgnoreCase)
                    .ToList();

                var totalStudents = query.Count();

                return Ok(new
                {
                    totalItems = totalStudents,
                    totalPages = (int)Math.Ceiling((double)totalStudents / pageSize),
                    currentPage = page,
                    pageSize = pageSize,
                    data = studentList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    code = 0,
                    message = "Có lỗi sảy ra",
                    error = ex.Message
                });
            }
        }


        [HttpGet("GetStudent/{id}")]
        public async Task<IActionResult> GetStudent(Guid id)
        {
            try
            {
                
                var student = await _dbContext.students
                    .FirstOrDefaultAsync(s => s.StudentId == id && s.IsDeleted == 0);

                if (student == null)
                {

                    return NotFound(new
                    {
                        code = 0,
                        message = "Không tìm thấy thông tin học sinh"
                    });
                }

                return Ok(new
                {
                    code = 1,
                    data = student
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    code = 0,
                    message = "Có lỗi xảy ra",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("DeleteStudent/{id}")]
        public async Task<IActionResult> DeleteStudent(Guid id)
        {
         
            var student = _dbContext.students
                    .FirstOrDefault(s => s.StudentId == id && s.IsDeleted == 0);

            if (student == null)
            {
                return NotFound(new
                {
                    code = 0,
                    message = "Không tìm thấy thôn tin học sinh"
                });
            }

            try
            {
           
                student.IsDeleted = 1;

                await _dbContext.students.Update(student);

                return Ok(new { code = 1, message = "Xóa học sinh thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 0, message = "Xóa học sinh thất bại", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(Guid id, [FromForm] AddStudentRequest request, IFormFile? avatar)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || !Regex.IsMatch(request.FirstName, @"^[a-zA-ZÀ-ỹà-ỹ\s\d]+$"))
            {
                return BadRequest(new { code = 0, message = "Họ tên đệm học sinh không đúng định dạng" });
            }

            if (string.IsNullOrWhiteSpace(request.LastName) || !Regex.IsMatch(request.LastName, @"^[a-zA-ZÀ-ỹà-ỹ\d]+$"))
            {
                return BadRequest(new { code = 0, message = "Tên học sinh không đúng định dạng" });
            }

            if (request.Gender.HasValue && request.Gender != 1 && request.Gender != 2)
            {
                return BadRequest(new { code = 0, message = "Giá trị giới tính không hợp lệ" });
            }


            if (!string.IsNullOrWhiteSpace(request.DayOfBirth))
            {

                if (!Regex.IsMatch(request.DayOfBirth, @"^\d{2}/\d{2}/\d{4}$"))
                {
                    return BadRequest(new { code = 0, message = "Ngày sinh không đúng định dạng dd/MM/yyyy" });
                }

                if (!DateTime.TryParseExact(request.DayOfBirth, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    return BadRequest(new { code = 0, message = "Ngày sinh không đúng định dạng dd/MM/yyyy" });
                }
            }


            if (avatar != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(avatar.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { code = 0, message = "Ảnh chỉ chấp nhập file .jpg, .jpeg, .png" });
                }
            }

            try
            {
                var student = _dbContext.students.FirstOrDefault(s => s.StudentId == id && s.IsDeleted == 0);
                if (student == null)
                {
                    return NotFound(new { code = 0, message = "Không tìm thấy thông tin học sinh" });
                }

                var existingClass = _dbContext.classes.FirstOrDefault(c => c.CLassId == request.ClassId);
                if (existingClass == null)
                {
                    return BadRequest(new { code = 0, message = "Thông tin lớp học không chính xác" });
                }

                string avatarPath = student.Avatar;
                if (avatar != null)
                {
                    string uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

                    if (!Directory.Exists(uploadDirectory))
                    {
                        Directory.CreateDirectory(uploadDirectory);
                    }

                    string fileExtension = Path.GetExtension(avatar.FileName);

                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(avatar.FileName);

                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                    string newFileName = $"{fileNameWithoutExtension}_{timestamp}{fileExtension}";

                    avatarPath = Path.Combine("Uploads/", newFileName);

                    using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), avatarPath), FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }
                }

                student.FirstName = request.FirstName;
                student.LastName = request.LastName;
                student.ClassId = request.ClassId;
                student.Gender = request.Gender ?? 1;
                student.DayOfBirth = request.DayOfBirth ?? "";
                student.Avatar = avatarPath;

                await _dbContext.students.Update(student);

                var dataStudent = _dbContext.students.Where(data => data.StudentId == id).FirstOrDefault();

                return Ok(new { code = 1, message = "Cập nhật thông tin học sinh thành công", data = dataStudent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 0, message = "Cập nhật thông tin học sinh thất bại", error = ex.Message });
            }
        }
    }
}
