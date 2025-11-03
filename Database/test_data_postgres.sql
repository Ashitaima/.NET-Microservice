-- ========================================
-- Test Data for Art Auction Platform (PostgreSQL)
-- ========================================

-- ========================================
-- 1. Вставка тестових користувачів
-- ========================================
INSERT INTO users (user_id, user_name, balance) VALUES
(1, 'John Doe', 10000.00),
(2, 'Jane Smith', 15000.00),
(3, 'Bob Wilson', 8000.00),
(4, 'Alice Johnson', 20000.00)
ON CONFLICT (user_id) DO NOTHING;

-- ========================================
-- 2. Вставка тестових аукціонів
-- ========================================
INSERT INTO auctions (artwork_id, artwork_name, seller_user_id, start_price, current_price, start_time, end_time, status)
VALUES
(100, 'Starry Night Replica', 1, 1000.00, 1000.00, '2025-02-01 10:00:00', '2025-02-10 18:00:00', 1), -- Active
(101, 'Mona Lisa Study', 2, 1500.00, 1500.00, '2025-02-02 09:00:00', '2025-02-12 17:00:00', 1), -- Active
(102, 'Abstract Dreams', 3, 800.00, 800.00, '2025-01-20 08:00:00', '2025-01-30 20:00:00', 2), -- Finished
(103, 'Digital Landscape', 1, 2000.00, 2000.00, '2025-02-05 12:00:00', '2025-02-15 16:00:00', 0); -- Pending

-- ========================================
-- 3. Вставка тестових ставок
-- ========================================
-- Ставки на аукціон 1 (Starry Night Replica)
INSERT INTO bids (auction_id, user_id, bid_amount, timestamp) VALUES
(1, 2, 1200.00, '2025-02-01 11:00:00'),
(1, 3, 1300.00, '2025-02-01 12:00:00'),
(1, 2, 1500.00, '2025-02-01 13:00:00');

-- Оновлюємо поточну ціну та переможця аукціону 1
UPDATE auctions SET current_price = 1500.00, winner_user_id = 2 WHERE auction_id = 1;

-- Ставки на аукціон 2 (Mona Lisa Study)
INSERT INTO bids (auction_id, user_id, bid_amount, timestamp) VALUES
(2, 1, 1600.00, '2025-02-02 10:00:00'),
(2, 4, 1800.00, '2025-02-02 11:00:00');

-- Оновлюємо поточну ціну та переможця аукціону 2
UPDATE auctions SET current_price = 1800.00, winner_user_id = 4 WHERE auction_id = 2;

-- Ставки на завершений аукціон 3
INSERT INTO bids (auction_id, user_id, bid_amount, timestamp) VALUES
(3, 2, 850.00, '2025-01-25 14:00:00'),
(3, 3, 900.00, '2025-01-26 15:00:00'),
(3, 2, 950.00, '2025-01-27 16:00:00');

-- Оновлюємо завершений аукціон
UPDATE auctions SET current_price = 950.00, winner_user_id = 2 WHERE auction_id = 3;

-- ========================================
-- 4. Додавання аукціонів до Watchlist
-- ========================================
INSERT INTO auction_watchlist (user_id, auction_id, added_at) VALUES
(1, 2, CURRENT_TIMESTAMP), -- John watches Mona Lisa Study
(2, 1, CURRENT_TIMESTAMP), -- Jane watches Starry Night
(3, 1, CURRENT_TIMESTAMP), -- Bob watches Starry Night
(4, 2, CURRENT_TIMESTAMP); -- Alice watches Mona Lisa Study

-- ========================================
-- 5. Створення платежу для завершеного аукціону
-- ========================================
INSERT INTO payments (auction_id, user_id, amount, payment_time, transaction_status) VALUES
(3, 2, 950.00, CURRENT_TIMESTAMP, 1); -- Success

-- Оновлюємо статус аукціону на Paid
UPDATE auctions SET status = 3 WHERE auction_id = 3;

-- ========================================
-- 6. Перевірочні запити
-- ========================================
-- Всі користувачі
SELECT * FROM users;

-- Всі аукціони з іменами переможців
SELECT 
    a.auction_id,
    a.artwork_name,
    a.current_price,
    a.status,
    u.user_name AS winner_name
FROM auctions a
LEFT JOIN users u ON a.winner_user_id = u.user_id;

-- Всі ставки з деталями
SELECT 
    b.bid_id,
    b.bid_amount,
    b.timestamp,
    a.artwork_name,
    u.user_name
FROM bids b
JOIN auctions a ON b.auction_id = a.auction_id
JOIN users u ON b.user_id = u.user_id
ORDER BY b.timestamp DESC;

-- Тестування function sp_get_user_bids
SELECT * FROM sp_get_user_bids(2);

DO $$
BEGIN
    RAISE NOTICE 'All test data inserted and verified!';
END $$;
