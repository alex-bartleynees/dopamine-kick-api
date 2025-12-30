using System.ComponentModel.DataAnnotations;

namespace Users.Domain.Entities;

public class User
{
    [Key] 
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; }

    public string Name { get; set; }

    public string Image { get; set; }

    public User(string name, string username, string image)
    {
        Name = name;
        Username = username;
        Image = image;
    }
}