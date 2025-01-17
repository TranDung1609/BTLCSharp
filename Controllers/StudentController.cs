using BTL.Middleware;
using BTL.Models;
using BTL.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

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
            if (request == null || string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.ClassId))
            {
                return BadRequest(new { code = 0, message = "Invalid student data" });
            }

            // Kiểm tra ClassId có hợp lệ không
            if (!ObjectId.TryParse(request.ClassId, out var objectId))
            {
                return BadRequest(new { code = 0, message = "Invalid ClassId format" });
            }

            try
            {
                // Kiểm tra ClassId có tồn tại trong cơ sở dữ liệu hay không
                var existingClass = _dbContext.classes.Where(c => c.Id == objectId).FirstOrDefault();
                if (existingClass == null)
                {
                    return BadRequest(new { code = 0, message = "Thông tin lớp học không chính xác" });
                }

                // Xử lý file avatar
                string avatarPath = null;
                if (avatar != null)
                {
                    string uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                    if (!Directory.Exists(uploadDirectory))
                    {
                        Directory.CreateDirectory(uploadDirectory);
                    }

                    avatarPath = Path.Combine(uploadDirectory, avatar.FileName);
                    using (var stream = new FileStream(avatarPath, FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }
                }

                // Tạo đối tượng Student từ request
                var student = new Student
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    ClassId = request.ClassId,
                    Gender = request.Gender,
                    DayOfBirth = request.DayOfBirth,
                    Avatar = avatarPath
                };

                // Lưu vào database
                await _dbContext.students.Insert(student);

                return Ok(new { code = 1, message = "Student added successfully", data = student });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 0, message = "An error occurred", error = ex.Message });
            }
        }

        [HttpGet("GetStudents")]
        public async Task<IActionResult> GetStudents([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string classId = null)
        {
            try
            {
                var skip = (page - 1) * pageSize;

                // Xây dựng truy vấn tìm học sinh
                var query = _dbContext.students.Where(student => student.IsDeleted == 0);

                // Nếu có classId thì thêm điều kiện vào query
                if (!string.IsNullOrEmpty(classId))
                {
                    query = query.Where(student => student.ClassId == classId);
                }

                query = query.OrderBy(student => student.LastName);

                // Lấy danh sách học sinh với phân trang
                var studentList = query
                    .Skip(skip)
                    .Take(pageSize)
                    .ToArray();

                // Đếm tổng số học sinh thỏa mãn điều kiện
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
                    message = "An error occurred",
                    error = ex.Message
                });
            }
        }


        [HttpGet("GetStudent/{id}")]
        public async Task<IActionResult> GetStudent(string id)
        {
            try
            {
                // Chuyển chuỗi id thành ObjectId
                if (!ObjectId.TryParse(id, out var objectId))
                {
                    return BadRequest(new
                    {
                        code = 0,
                        message = "Invalid student id"
                    });
                }

                // Tìm học sinh theo Id
                var student = _dbContext.students
                    .FirstOrDefault(s => s.Id == objectId && s.IsDeleted == 0);

                // Kiểm tra nếu không tìm thấy học sinh
                if (student == null)
                {
                    return NotFound(new
                    {
                        code = 0,
                        message = "Student not found"
                    });
                }

                // Trả về thông tin học sinh
                return Ok(new
                {
                    code = 1,
                    message = "Student retrieved successfully",
                    data = student
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    code = 0,
                    message = "An error occurred",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("DeleteStudent/{id}")]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest(new
                {
                    code = 0,
                    message = "Invalid student id"
                });
            }

            var student = _dbContext.students
                    .FirstOrDefault(s => s.Id == objectId && s.IsDeleted == 0);

            if (student == null)
            {
                return NotFound(new
                {
                    code = 0,
                    message = "Student not found"
                });
            }

            try
            {
           
                student.IsDeleted = 1;

                // Lưu thay đổi
                await _dbContext.students.Update(student);

                return Ok(new { code = 1, message = "Student deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 0, message = "An error occurred", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(string id, [FromForm] AddStudentRequest request, IFormFile? avatar)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.ClassId))
            {
                return BadRequest(new { code = 0, message = "Invalid student data" });
            }

            // Kiểm tra định dạng ClassId
            if (!ObjectId.TryParse(request.ClassId, out var classObjectId))
            {
                return BadRequest(new { code = 0, message = "Invalid ClassId format" });
            }

            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest(new
                {
                    code = 0,
                    message = "Invalid student id"
                });
            }

            try
            {
                // Tìm kiếm học sinh dựa vào id
                var student = _dbContext.students.FirstOrDefault(s => s.Id == objectId && s.IsDeleted == 0);
                if (student == null)
                {
                    return NotFound(new { code = 0, message = "Student not found" });
                }

                // Kiểm tra ClassId có tồn tại trong cơ sở dữ liệu hay không
                var existingClass = _dbContext.classes.FirstOrDefault(c => c.Id == classObjectId);
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

                    avatarPath = Path.Combine(uploadDirectory, avatar.FileName);
                    using (var stream = new FileStream(avatarPath, FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }
                }

                student.FirstName = request.FirstName;
                student.LastName = request.LastName;
                student.ClassId = request.ClassId;
                student.Gender = request.Gender;
                student.DayOfBirth = request.DayOfBirth;
                student.Avatar = avatarPath;

                await _dbContext.students.Update(student);

                return Ok(new { code = 1, message = "Student updated successfully", data = student });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 0, message = "An error occurred", error = ex.Message });
            }
        }
        
    }
}
