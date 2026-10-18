-- Atualização única após TokensReutilizaveis.sql, com API parada e backup.
-- Bases novas criadas com EnsureCreated já possuem estas colunas.
BEGIN IMMEDIATE;
ALTER TABLE Jogadores ADD COLUMN AtivacaoHash TEXT NULL;
ALTER TABLE Jogadores ADD COLUMN AtivadoEmUtc TEXT NULL;
ALTER TABLE ParticipacoesPartida ADD COLUMN WebhookUrl TEXT NULL;
COMMIT;
