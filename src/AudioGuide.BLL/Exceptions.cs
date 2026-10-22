namespace AudioGuide.BLL;

/// <summary>Lỗi nghiệp vụ / dữ liệu không hợp lệ → API trả 400.</summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

/// <summary>Không tìm thấy dữ liệu → API trả 404.</summary>
public class NotFoundException : BusinessException
{
    public NotFoundException(string message) : base(message) { }
}
