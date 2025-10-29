namespace SecureApi.DTOs;

public record LoginRequest(string Correo, string Contrasena);
public record LoginResponse(string Token);
