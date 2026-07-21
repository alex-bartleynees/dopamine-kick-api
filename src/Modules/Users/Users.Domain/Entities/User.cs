using SharedKernel.Abstractions;

namespace Users.Domain.Entities;

public class User : IAuditable
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Image { get; set; } = string.Empty;
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }

    public User() { }

    public User(string name, string username, string image)
    {
        Name = name;
        Username = username;
        Image = image;
    }
}