namespace AuctionService.Domain.Entities;

/// <summary>
/// Статус аукціону
/// </summary>
public enum AuctionStatus
{
    /// <summary>
    /// Очікує початку
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Активний
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Завершений
    /// </summary>
    Finished = 2,
    
    /// <summary>
    /// Оплачений
    /// </summary>
    Paid = 3
}
