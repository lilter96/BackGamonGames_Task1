CREATE EXTENSION IF NOT EXISTS dblink;

DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'rpsdb') THEN
            PERFORM dblink_exec('dbname=postgres','CREATE DATABASE rpsdb');
        END IF;
    END
$$;

CREATE TABLE IF NOT EXISTS "Users"
(
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(50) NOT NULL,
    "Balance" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS "MatchHistories"
(
    "Id" SERIAL PRIMARY KEY,
    "RoomName" VARCHAR(100) NOT NULL,
    "Bet" NUMERIC(18,2) NOT NULL DEFAULT 0,

    "Player1Id" INT NOT NULL,
    "Player2Id" INT,
    "Player1Move" VARCHAR(1),
    "Player2Move" VARCHAR(1),
    "WinnerId" INT,

    "IsEnded" BOOLEAN NOT NULL DEFAULT FALSE,

    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT "FK_MatchHistories_Player1" FOREIGN KEY ("Player1Id") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_MatchHistories_Player2" FOREIGN KEY ("Player2Id") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_MatchHistories_Winner"  FOREIGN KEY ("WinnerId")  REFERENCES "Users" ("Id")
);

CREATE TABLE IF NOT EXISTS "GameTransactions"
(
    "Id" SERIAL PRIMARY KEY,
    "FromUserId" INT,
    "ToUserId" INT NOT NULL,
    "Amount" NUMERIC(18,2) NOT NULL,
    "TransactionType" VARCHAR(50),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_GameTransactions_FromUser"
        FOREIGN KEY ("FromUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_GameTransactions_ToUser"
        FOREIGN KEY ("ToUserId") REFERENCES "Users" ("Id")
);

CREATE INDEX IF NOT EXISTS IX_Users_Username         ON "Users" ("Username");

CREATE INDEX IF NOT EXISTS IX_MatchHistories_Player1Id      ON "MatchHistories" ("Player1Id");
CREATE INDEX IF NOT EXISTS IX_MatchHistories_Player2Id      ON "MatchHistories" ("Player2Id");
CREATE INDEX IF NOT EXISTS IX_MatchHistories_WinnerId       ON "MatchHistories" ("WinnerId");
CREATE INDEX IF NOT EXISTS IX_MatchHistories_RoomName       ON "MatchHistories" ("RoomName");

CREATE INDEX IF NOT EXISTS IX_GameTransactions_From  ON "GameTransactions" ("FromUserId");
CREATE INDEX IF NOT EXISTS IX_GameTransactions_To    ON "GameTransactions" ("ToUserId");
