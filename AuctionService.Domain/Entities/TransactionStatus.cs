namespace AuctionService.Domain.Entities;

/// <summary>
/// Статус транзакції платежу
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Обробляється
    /// </summary>
    Processing = 0,
    
    /// <summary>
    /// Успішно
    /// </summary>
    Success = 1,
    
    /// <summary>
    /// Невдало
    /// </summary>
    Failed = 2
}
