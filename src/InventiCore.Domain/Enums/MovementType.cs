namespace InventiCore.Domain.Enums;

public enum MovementType
{
    Entry = 1,      // Entrada de mercadoria (compra, devolução de cliente)
    Exit = 2,       // Saída de mercadoria (venda, envio para filial)
    Transfer = 3,   // Transferência entre depósitos
    Adjustment = 4  // Ajuste de inventário (correção manual, perda, avaria)
}
