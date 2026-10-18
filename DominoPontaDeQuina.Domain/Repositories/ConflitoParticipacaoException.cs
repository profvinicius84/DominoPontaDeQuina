namespace DominoPontaDeQuina.Domain.Repositories;

/// <summary>A ativação ou a ocupação do jogador mudou antes da confirmação.</summary>
public sealed class ConflitoParticipacaoException : Exception;
