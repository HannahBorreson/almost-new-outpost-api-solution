-- =============================================================================
--  Almost New 24/7 Outpost — database build script
-- -----------------------------------------------------------------------------
--  Target   : MySQL 8.4 LTS (also works on MySQL 8.0.16 or later)
--  Creates  : app login  outpost_app@localhost
--             database   almost_new_outpost
--             5 tables, 1 view, 10 stored procedures, seed data
--
--  !! WARNING !!  This script DROPS and RE-CREATES the database.
--                 Running it again is a full reset: every purchase and every
--                 product change is lost. That is intentional for class use.
--
--  How to run (as root):
--    MySQL Workbench : File > Open SQL Script... > this file > Execute (lightning bolt)
--    PowerShell      : & "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" -u root -p `
--                        -e "source C:/path/to/almost_new_outpost.sql"
--    (PowerShell has no "<" redirection, so use -e "source ..." as shown.)
--
--  Seeded logins (password = username + "123"):
--    citizen / citizen123     Citizen
--    scout   / scout123       Scout
--    medic   / medic123       Medic
--    security/ security123    Security
--    admin   / admin123       MerchantAdmin
--
--  Design notes
--    * Prices are whole credits (INT). The weather surcharge is NOT stored;
--      the API computes it. Purchases.FinalPrice is a snapshot of what was
--      actually charged, so history never changes when prices change.
--    * Which role may buy which category is a business rule in the API
--      (CategoryAccessPolicy), not a table. The API checks it BEFORE it
--      calls sp_CreatePurchase.
--    * Password hashes are ASP.NET Core Identity PasswordHasher v3 format
--      (PBKDF2-HMAC-SHA512, 100,000 iterations, 16-byte salt, 32-byte key).
--    * Stored procedures report rule violations with SIGNAL. The API maps
--      the MYSQL_ERRNO values to HTTP status codes:
--        50001 PRODUCT_NOT_FOUND        -> 404
--        50002 CATEGORY_NOT_FOUND       -> 400 (field: categoryId)
--        50003 DUPLICATE_PRODUCT_NAME   -> 409
--        50004 PRODUCT_HAS_PURCHASES    -> 409
--        50005 USER_NOT_FOUND           -> 401
--        50006 INVALID_PRICE            -> 500 (an API bug: price must be >= 1)
--        50010 INVALID_SORT_BY          -> 400 (field: sortBy)
--        50011 INVALID_SORT_DIR         -> 400 (field: sortDir)
-- =============================================================================


-- =============================================================================
-- 1. Application login
--    The API connects as outpost_app, which can read/write data and run the
--    procedures but cannot change the schema. root is for admin work only.
--    Change the password here (and in the API's user-secrets) if you like.
-- =============================================================================
CREATE USER IF NOT EXISTS 'outpost_app'@'localhost' IDENTIFIED BY 'Outpost_Dev_2026!';
ALTER USER 'outpost_app'@'localhost' IDENTIFIED BY 'Outpost_Dev_2026!';

-- Dev database and the test database the API test suite builds for itself.
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON almost_new_outpost.*      TO 'outpost_app'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON almost_new_outpost_test.* TO 'outpost_app'@'localhost';


-- =============================================================================
-- 2. Database
-- =============================================================================
DROP DATABASE IF EXISTS almost_new_outpost;

CREATE DATABASE almost_new_outpost
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;   -- case-insensitive: 'Medic' = 'medic'

USE almost_new_outpost;


-- =============================================================================
-- 3. Tables
-- =============================================================================

-- Who a user is in the outpost. Drives what they may buy.
CREATE TABLE Roles (
    RoleId    INT          NOT NULL AUTO_INCREMENT,
    RoleName  VARCHAR(32)  NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
) ENGINE = InnoDB;

-- People who can log in. One role each.
CREATE TABLE Users (
    UserId        INT           NOT NULL AUTO_INCREMENT,
    Username      VARCHAR(50)   NOT NULL,
    PasswordHash  VARCHAR(255)  NOT NULL,
    RoleId        INT           NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId)
        REFERENCES Roles (RoleId) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB;

-- Groups of products (Basic, Exploration, Medical, Security).
CREATE TABLE Categories (
    CategoryId    INT          NOT NULL AUTO_INCREMENT,
    CategoryName  VARCHAR(50)  NOT NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_CategoryName UNIQUE (CategoryName)
) ENGINE = InnoDB;

-- Items for sale. PriceCredits is the BASE price (clear weather).
CREATE TABLE Products (
    ProductId     INT           NOT NULL AUTO_INCREMENT,
    Name          VARCHAR(100)  NOT NULL,
    Description   VARCHAR(500)  NULL,
    CategoryId    INT           NOT NULL,
    PriceCredits  INT           NOT NULL,
    IconKey       VARCHAR(50)   NULL,      -- the UI maps this to an icon
    CONSTRAINT PK_Products PRIMARY KEY (ProductId),
    CONSTRAINT UQ_Products_Name UNIQUE (Name),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
        REFERENCES Categories (CategoryId) ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT CK_Products_PriceCredits CHECK (PriceCredits BETWEEN 1 AND 100000)
) ENGINE = InnoDB;

-- One row per item bought. FinalPrice = what was actually charged.
CREATE TABLE Purchases (
    PurchaseId    INT          NOT NULL AUTO_INCREMENT,
    UserId        INT          NOT NULL,
    ProductId     INT          NOT NULL,
    PurchaseDate  DATETIME(6)  NOT NULL,   -- always UTC
    FinalPrice    INT          NOT NULL,
    CONSTRAINT PK_Purchases PRIMARY KEY (PurchaseId),
    CONSTRAINT FK_Purchases_Users FOREIGN KEY (UserId)
        REFERENCES Users (UserId) ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT FK_Purchases_Products FOREIGN KEY (ProductId)
        REFERENCES Products (ProductId) ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT CK_Purchases_FinalPrice CHECK (FinalPrice >= 1)
) ENGINE = InnoDB;

-- Purchase history is always "this user's rows, newest first".
CREATE INDEX IX_Purchases_UserId_PurchaseDate ON Purchases (UserId, PurchaseDate DESC);


-- =============================================================================
-- 4. View
--    One place that defines "a product as the API returns it" (with its
--    category name), so every product procedure returns the same columns.
-- =============================================================================
CREATE VIEW vw_ProductDetails AS
SELECT
    p.ProductId,
    p.Name,
    p.Description,
    p.CategoryId,
    c.CategoryName,
    p.PriceCredits,
    p.IconKey
FROM Products p
INNER JOIN Categories c ON c.CategoryId = p.CategoryId;


-- =============================================================================
-- 5. Stored procedures
-- =============================================================================
DELIMITER $$

-- ---------------------------------------------------------------------------
-- Users
-- ---------------------------------------------------------------------------

-- Login lookup. Returns 0 or 1 row. The API verifies the password hash.
CREATE PROCEDURE sp_GetUserByUsername (
    IN p_Username VARCHAR(50)
)
BEGIN
    SELECT u.UserId, u.Username, u.PasswordHash, u.RoleId, r.RoleName
    FROM Users u
    INNER JOIN Roles r ON r.RoleId = u.RoleId
    WHERE u.Username = TRIM(p_Username);
END$$

-- Used by GET /api/auth/me. Never returns the password hash.
CREATE PROCEDURE sp_GetUserById (
    IN p_UserId INT
)
BEGIN
    SELECT u.UserId, u.Username, u.RoleId, r.RoleName
    FROM Users u
    INNER JOIN Roles r ON r.RoleId = u.RoleId
    WHERE u.UserId = p_UserId;
END$$

-- ---------------------------------------------------------------------------
-- Categories
-- ---------------------------------------------------------------------------

CREATE PROCEDURE sp_GetCategories ()
BEGIN
    SELECT CategoryId, CategoryName
    FROM Categories
    ORDER BY CategoryId;
END$$

-- ---------------------------------------------------------------------------
-- Products
-- ---------------------------------------------------------------------------

-- Search by name and sort. Every role sees every product.
--   p_Search  : text to find inside the name; NULL or '' = all products
--   p_SortBy  : 'name' or 'price'   (NULL = 'name')
--   p_SortDir : 'asc'  or 'desc'    (NULL = 'asc')
-- Sorting by base price gives the same order as current price, because the
-- weather surcharge is the same percentage for every product.
CREATE PROCEDURE sp_SearchProducts (
    IN p_Search  VARCHAR(50),
    IN p_SortBy  VARCHAR(10),
    IN p_SortDir VARCHAR(4)
)
BEGIN
    DECLARE v_Search  VARCHAR(50) DEFAULT TRIM(IFNULL(p_Search, ''));
    DECLARE v_SortBy  VARCHAR(10) DEFAULT LOWER(IFNULL(p_SortBy, 'name'));
    DECLARE v_SortDir VARCHAR(4)  DEFAULT LOWER(IFNULL(p_SortDir, 'asc'));
    DECLARE v_Pattern VARCHAR(160);

    IF v_SortBy NOT IN ('name', 'price') THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'INVALID_SORT_BY', MYSQL_ERRNO = 50010;
    END IF;

    IF v_SortDir NOT IN ('asc', 'desc') THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'INVALID_SORT_DIR', MYSQL_ERRNO = 50011;
    END IF;

    -- Escape LIKE wildcards so a search for "50%" means the text "50%",
    -- not "anything starting with 50". Backslash is LIKE's escape character.
    SET v_Pattern = CONCAT('%',
        REPLACE(REPLACE(REPLACE(v_Search, '\\', '\\\\'), '%', '\\%'), '_', '\\_'),
        '%');

    SELECT ProductId, Name, Description, CategoryId, CategoryName, PriceCredits, IconKey
    FROM vw_ProductDetails
    WHERE v_Search = '' OR Name LIKE v_Pattern
    ORDER BY
        CASE WHEN v_SortBy = 'name'  AND v_SortDir = 'asc'  THEN Name         END ASC,
        CASE WHEN v_SortBy = 'name'  AND v_SortDir = 'desc' THEN Name         END DESC,
        CASE WHEN v_SortBy = 'price' AND v_SortDir = 'asc'  THEN PriceCredits END ASC,
        CASE WHEN v_SortBy = 'price' AND v_SortDir = 'desc' THEN PriceCredits END DESC,
        Name ASC;   -- tie-breaker: equal prices list alphabetically
END$$

-- One product (details modal, purchase check). Returns 0 or 1 row.
CREATE PROCEDURE sp_GetProductById (
    IN p_ProductId INT
)
BEGIN
    SELECT ProductId, Name, Description, CategoryId, CategoryName, PriceCredits, IconKey
    FROM vw_ProductDetails
    WHERE ProductId = p_ProductId;
END$$

-- Admin: add a product. Returns the new product row.
CREATE PROCEDURE sp_CreateProduct (
    IN p_Name         VARCHAR(100),
    IN p_Description  VARCHAR(500),
    IN p_CategoryId   INT,
    IN p_PriceCredits INT,
    IN p_IconKey      VARCHAR(50)
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryId = p_CategoryId) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'CATEGORY_NOT_FOUND', MYSQL_ERRNO = 50002;
    END IF;

    IF EXISTS (SELECT 1 FROM Products WHERE Name = TRIM(p_Name)) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'DUPLICATE_PRODUCT_NAME', MYSQL_ERRNO = 50003;
    END IF;

    INSERT INTO Products (Name, Description, CategoryId, PriceCredits, IconKey)
    VALUES (
        TRIM(p_Name),
        NULLIF(TRIM(p_Description), ''),   -- empty text is stored as NULL
        p_CategoryId,
        p_PriceCredits,
        NULLIF(TRIM(p_IconKey), '')
    );

    SELECT ProductId, Name, Description, CategoryId, CategoryName, PriceCredits, IconKey
    FROM vw_ProductDetails
    WHERE ProductId = LAST_INSERT_ID();
END$$

-- Admin: replace every editable field of a product. Returns the updated row.
CREATE PROCEDURE sp_UpdateProduct (
    IN p_ProductId    INT,
    IN p_Name         VARCHAR(100),
    IN p_Description  VARCHAR(500),
    IN p_CategoryId   INT,
    IN p_PriceCredits INT,
    IN p_IconKey      VARCHAR(50)
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductId = p_ProductId) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'PRODUCT_NOT_FOUND', MYSQL_ERRNO = 50001;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryId = p_CategoryId) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'CATEGORY_NOT_FOUND', MYSQL_ERRNO = 50002;
    END IF;

    -- Another product may not already use this name.
    IF EXISTS (SELECT 1 FROM Products
               WHERE Name = TRIM(p_Name) AND ProductId <> p_ProductId) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'DUPLICATE_PRODUCT_NAME', MYSQL_ERRNO = 50003;
    END IF;

    UPDATE Products
    SET Name         = TRIM(p_Name),
        Description  = NULLIF(TRIM(p_Description), ''),
        CategoryId   = p_CategoryId,
        PriceCredits = p_PriceCredits,
        IconKey      = NULLIF(TRIM(p_IconKey), '')
    WHERE ProductId = p_ProductId;

    SELECT ProductId, Name, Description, CategoryId, CategoryName, PriceCredits, IconKey
    FROM vw_ProductDetails
    WHERE ProductId = p_ProductId;
END$$

-- Admin: delete a product. Refused if anyone has bought it, because
-- purchase history must keep pointing at a real product.
CREATE PROCEDURE sp_DeleteProduct (
    IN p_ProductId INT
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductId = p_ProductId) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'PRODUCT_NOT_FOUND', MYSQL_ERRNO = 50001;
    END IF;

    IF EXISTS (SELECT 1 FROM Purchases WHERE ProductId = p_ProductId) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'PRODUCT_HAS_PURCHASES', MYSQL_ERRNO = 50004;
    END IF;

    DELETE FROM Products WHERE ProductId = p_ProductId;
END$$

-- ---------------------------------------------------------------------------
-- Purchases
-- ---------------------------------------------------------------------------

-- Record one purchase and return it.
-- The API has ALREADY checked that the user's role may buy this product's
-- category, and has ALREADY computed p_FinalPrice from the current weather.
-- The purchase time is set here, on the database clock, in UTC.
CREATE PROCEDURE sp_CreatePurchase (
    IN p_UserId     INT,
    IN p_ProductId  INT,
    IN p_FinalPrice INT
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId = p_UserId) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'USER_NOT_FOUND', MYSQL_ERRNO = 50005;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductId = p_ProductId) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'PRODUCT_NOT_FOUND', MYSQL_ERRNO = 50001;
    END IF;

    IF p_FinalPrice IS NULL OR p_FinalPrice < 1 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'INVALID_PRICE', MYSQL_ERRNO = 50006;
    END IF;

    INSERT INTO Purchases (UserId, ProductId, PurchaseDate, FinalPrice)
    VALUES (p_UserId, p_ProductId, UTC_TIMESTAMP(6), p_FinalPrice);

    SELECT pu.PurchaseId, pu.ProductId, pr.Name AS ProductName,
           pu.PurchaseDate, pu.FinalPrice
    FROM Purchases pu
    INNER JOIN Products pr ON pr.ProductId = pu.ProductId
    WHERE pu.PurchaseId = LAST_INSERT_ID();
END$$

-- One user's purchase history, newest first.
--   p_Limit : how many rows; NULL = 50; values are kept between 1 and 100.
CREATE PROCEDURE sp_GetPurchasesByUser (
    IN p_UserId INT,
    IN p_Limit  INT
)
BEGIN
    DECLARE v_Limit INT DEFAULT IFNULL(p_Limit, 50);

    IF v_Limit < 1   THEN SET v_Limit = 1;   END IF;
    IF v_Limit > 100 THEN SET v_Limit = 100; END IF;

    SELECT pu.PurchaseId, pu.ProductId, pr.Name AS ProductName,
           pu.PurchaseDate, pu.FinalPrice
    FROM Purchases pu
    INNER JOIN Products pr ON pr.ProductId = pu.ProductId
    WHERE pu.UserId = p_UserId
    ORDER BY pu.PurchaseDate DESC, pu.PurchaseId DESC
    LIMIT v_Limit;
END$$

DELIMITER ;


-- =============================================================================
-- 6. Seed data
--    IDs are explicit so they match the UI's mock data.
-- =============================================================================

INSERT INTO Roles (RoleId, RoleName) VALUES
    (1, 'Citizen'),
    (2, 'Scout'),
    (3, 'Medic'),
    (4, 'Security'),
    (5, 'MerchantAdmin');

INSERT INTO Categories (CategoryId, CategoryName) VALUES
    (1, 'Basic'),
    (2, 'Exploration'),
    (3, 'Medical'),
    (4, 'Security');

-- Password = username + "123". Hashes made with ASP.NET Core PasswordHasher v3.
INSERT INTO Users (UserId, Username, PasswordHash, RoleId) VALUES
    (1, 'citizen',  'AQAAAAIAAYagAAAAEDVCrYqcoRNp4spqIcJ2Zrw2dsdf0cOr/Z9GIi2KFBbnmKBlH0tWohOmBn7sLB3AYA==', 1),
    (2, 'scout',    'AQAAAAIAAYagAAAAEO/MJJxniU4diJ8SGyFdQDqyZSaDg83qqdw5mJ51TKDV3ZVl2JukxN60dEXgjwzKbw==', 2),
    (3, 'medic',    'AQAAAAIAAYagAAAAEBynTV8QCZ6sSYm4k0Cme5RTYAvVBcvPBbUvKz5XqSzfCcA3SF1hnaWqDu9k1DyiIw==', 3),
    (4, 'security', 'AQAAAAIAAYagAAAAEC79/A8dL1u5UY747XOhvuoKkMBAI/YwE0JuyQjTXad1ZF9hus7GcsQEVgRdvdtGTA==', 4),
    (5, 'admin',    'AQAAAAIAAYagAAAAEGMgop2z/2dyYjnhnHO5SkC9d2QxjwKOaRls77EzkXySFUbBmH25rLEvAX0LN3JV8A==', 5);

-- Base prices are multiples of 4, so the +25% surcharge is always whole credits.
INSERT INTO Products (ProductId, Name, Description, CategoryId, PriceCredits, IconKey) VALUES
    -- Basic
    ( 1, 'Clean Water',           'Two liters of filtered, sealed drinking water.',                     1,  12, 'water'),
    ( 2, 'Food Ration Pack',      'A day of shelf-stable meals, mostly within date.',                   1,  16, 'ration'),
    ( 3, 'Flashlight',            'Sturdy hand torch with a fresh bulb.',                               1,  20, 'flashlight'),
    ( 4, 'Batteries',             'A pack of four, tested and holding charge.',                         1,   8, 'battery'),
    ( 5, 'Blanket',               'Thick wool blanket, washed and patched.',                            1,  24, 'blanket'),
    ( 6, 'Cooking Kit',           'Pot, pan and a folding stove for campfire meals.',                   1,  32, 'cooking'),
    -- Exploration
    ( 7, 'Compass',               'Liquid-filled compass that still points true north.',                2,  36, 'compass'),
    ( 8, 'Binoculars',            'Clear 10x lenses for scouting the overgrown streets.',               2,  60, 'binoculars'),
    ( 9, 'Portable Water Filter', 'Turns river water into something you can drink.',                    2,  44, 'filter'),
    (10, 'Survival Backpack',     'Weatherproof pack with room for a week on the road.',                2,  80, 'backpack'),
    -- Medical
    (11, 'First Aid Kit',         'Plasters, antiseptic and gauze for everyday scrapes.',               3,  40, 'first-aid'),
    (12, 'Medical Supply Kit',    'Sterile dressings, splints and basic instruments.',                  3, 100, 'medical-kit'),
    (13, 'Bandages',              'A roll of clean cotton bandage.',                                    3,  12, 'bandage'),
    (14, 'Antibiotics',           'A short course of pre-collapse antibiotics, sealed.',                3,  76, 'pills'),
    -- Security
    (15, 'Stun Baton',            'Rechargeable baton for keeping the peace at the gate.',              4,  88, 'baton'),
    (16, 'Riot Shield',           'Clear polycarbonate shield, lightly scuffed.',                       4, 120, 'shield'),
    (17, 'Protective Vest',       'Padded vest for night patrols.',                                     4, 148, 'vest'),
    (18, 'Signal Flare Kit',      'Three bright flares to call for help.',                              4,  48, 'flare');

-- A few purchases so history pages are not empty. Times are relative to when
-- this script runs, so a reset always looks recent. Every row obeys the
-- role-to-category rules; rows at 125% were bought during dangerous weather.
INSERT INTO Purchases (UserId, ProductId, PurchaseDate, FinalPrice) VALUES
    (1,  1, UTC_TIMESTAMP(6) - INTERVAL 72 HOUR,   12),  -- citizen  : Clean Water
    (1,  5, UTC_TIMESTAMP(6) - INTERVAL 26 HOUR,   24),  -- citizen  : Blanket
    (2,  7, UTC_TIMESTAMP(6) - INTERVAL 48 HOUR,   36),  -- scout    : Compass
    (2, 10, UTC_TIMESTAMP(6) - INTERVAL  5 HOUR,  100),  -- scout    : Survival Backpack (storm)
    (3, 11, UTC_TIMESTAMP(6) - INTERVAL  4 HOUR,   40),  -- medic    : First Aid Kit
    (3, 13, UTC_TIMESTAMP(6) - INTERVAL  2 HOUR,   15),  -- medic    : Bandages (dust storm)
    (3, 12, UTC_TIMESTAMP(6) - INTERVAL 30 MINUTE, 125), -- medic    : Medical Supply Kit (dust storm)
    (4, 18, UTC_TIMESTAMP(6) - INTERVAL 24 HOUR,   48),  -- security : Signal Flare Kit
    (4,  4, UTC_TIMESTAMP(6) - INTERVAL  6 HOUR,    8),  -- security : Batteries
    (5,  6, UTC_TIMESTAMP(6) - INTERVAL  3 HOUR,   32);  -- admin    : Cooking Kit


-- =============================================================================
-- 7. Quick check — you should see 5, 5, 4, 18, 10
-- =============================================================================
SELECT
    (SELECT COUNT(*) FROM Roles)      AS Roles,
    (SELECT COUNT(*) FROM Users)      AS Users,
    (SELECT COUNT(*) FROM Categories) AS Categories,
    (SELECT COUNT(*) FROM Products)   AS Products,
    (SELECT COUNT(*) FROM Purchases)  AS Purchases;
