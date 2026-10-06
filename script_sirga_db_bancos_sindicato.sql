-- SIRGA: banco del sindicato y su configuracion de boleta. Idempotente: se puede correr las veces que haga falta.
IF NOT EXISTS (SELECT 1 FROM Banks WHERE Name = 'Banco Santiago del Estero')
    INSERT INTO Banks (Name, Branch, Notes, IsActive)
    VALUES ('Banco Santiago del Estero', 'Sucursal Central',
            'Banco con el que opera el S.E.C. Santiago del Estero. Codigo de ente 0134.', 1);

IF NOT EXISTS (SELECT 1 FROM UnionBankConfigs c INNER JOIN Banks b ON b.Id = c.BankId
               WHERE c.UnionId = 19 AND b.Name = 'Banco Santiago del Estero')
    INSERT INTO UnionBankConfigs (UnionId, BankId, ClientNumber, EntityCode, DesignFormat, FirstDueDay, DaysToSecondDue, IsActive)
    SELECT 19, Id, '18588639', '0134', 4, 15, 10, 1 FROM Banks WHERE Name = 'Banco Santiago del Estero';
