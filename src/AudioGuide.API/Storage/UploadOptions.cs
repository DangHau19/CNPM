namespace AudioGuide.API.Storage;

/// <summary>Thư mục lưu file do admin tải lên (âm thanh, ảnh). Được phục vụ tại đường dẫn /uploads.</summary>
public record UploadOptions(string RootPath);
