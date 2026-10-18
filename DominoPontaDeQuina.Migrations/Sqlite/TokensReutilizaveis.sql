-- Atualização única de base SQLite criada pela versão anterior com EnsureCreated.
-- Executar com a API parada e backup do arquivo. Não se aplica a bases novas.
BEGIN IMMEDIATE;

-- Não reativar credenciais que já estavam expiradas antes da mudança de política.
UPDATE TokensAcesso
SET RevogadoEmUtc = strftime('%Y-%m-%d %H:%M:%f', 'now')
WHERE RevogadoEmUtc IS NULL AND julianday(ExpiraEmUtc) <= julianday('now');

DROP INDEX IF EXISTS IX_TokensAcesso_UsuarioId_ExpiraEmUtc;
ALTER TABLE TokensAcesso DROP COLUMN ExpiraEmUtc;
CREATE INDEX IX_TokensAcesso_UsuarioId_RevogadoEmUtc
ON TokensAcesso (UsuarioId, RevogadoEmUtc);

COMMIT;
