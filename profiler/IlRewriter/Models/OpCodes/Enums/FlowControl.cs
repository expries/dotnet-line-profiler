// ReSharper disable InconsistentNaming

namespace IlRewriter.Models.OpCodes.Enums;

public enum FlowControl
{
    Branch,
    Break,
    Call,
    Cond_Branch,
    Meta,
    Next,
    Phi,
    Return,
    Throw
}