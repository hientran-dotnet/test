# TCP Client-Server với Mã Hóa

## Mô tả
Chương trình mô phỏng giao tiếp TCP giữa Client và Server với tính năng mã hóa dữ liệu:

### Cách mã hóa
- **Client**: Mã hóa dữ liệu gửi đi bằng cách dịch chuyển +2 ký tự (A→C, B→D, ...)
- **Server**: 
  - Giải mã dữ liệu từ client (dịch chuyển -2) 
  - Mã hóa phản hồi bằng cách dịch chuyển -2 ký tự (A→Y, B→Z, C→A, ...)
- **Client**: Giải mã phản hồi từ server (dịch chuyển +2)

### Cấu hình
- **Port**: 25650
- **IP**: 127.0.0.1 (localhost)
- **Shift Value**: 2

## Cách chạy

### 1. Chạy Server trước
```bash
cd Server
dotnet run
```

### 2. Chạy Client
```bash
cd Client  
dotnet run
```

## Ví dụ hoạt động

### Client gửi "HELLO":
1. Client: "HELLO" → mã hóa → "JGNNQ"
2. Server: nhận "JGNNQ" → giải mã → "HELLO"
3. Server: tạo phản hồi "Server đã nhận: HELLO" → mã hóa → "Qcptcp ôđ ljôl: JCNNM"
4. Client: nhận phản hồi mã hóa → giải mã → "Server đã nhận: HELLO"

### Kết quả mong đợi:
- Dữ liệu gửi/nhận được mã hóa qua network
- Cả client và server đều hiển thị được dữ liệu gốc sau khi giải mã
- Gõ "exit" để thoát

## Các file chính
- `Server/Server-Program.cs` - Chương trình server
- `Client/CLient-Program.cs` - Chương trình client
