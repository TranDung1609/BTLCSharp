namespace BTL.Request
{
    public class AddStudentRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ClassId { get; set; }
        public short? Gender { get; set; }
        public string? DayOfBirth { get; set; }
    }
}
