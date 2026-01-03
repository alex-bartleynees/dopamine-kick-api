namespace Common.Abstractions;

public interface IAuditable
{
   public DateTimeOffset CreatedAt { get; }
   
   public DateTimeOffset UpdatedAt { get; set; }
}