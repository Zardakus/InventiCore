namespace InventiCore.Domain.Exceptions;

/// <summary>
/// Exceção de domínio para quando a quantidade em estoque é insuficiente.
/// </summary>
public class InsufficientStockException : Exception
{
    public InsufficientStockException(string productName, int requested, int available)
        : base($"Estoque insuficiente para '{productName}'. Solicitado: {requested}, Disponível: {available}.")
    {
        ProductName = productName;
        Requested = requested;
        Available = available;
    }

    public string ProductName { get; }
    public int Requested { get; }
    public int Available { get; }
}
