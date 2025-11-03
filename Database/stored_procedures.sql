-- ========================================
-- Stored Procedures for Art Auction Platform (PostgreSQL)
-- ========================================

-- ========================================
-- SP 1: Розмістити ставку з блокуванням
-- ========================================
CREATE OR REPLACE FUNCTION sp_place_bid(
    p_auction_id BIGINT,
    p_user_id BIGINT,
    p_bid_amount DECIMAL(18, 2)
)
RETURNS TABLE (
    success INT,
    message TEXT
) AS $$
DECLARE
    v_current_price DECIMAL(18, 2);
    v_status INT;
    v_end_time TIMESTAMP;
BEGIN
    -- Блокуємо запис аукціону для оновлення
    SELECT current_price, status, end_time
    INTO v_current_price, v_status, v_end_time
    FROM auctions
    WHERE auction_id = p_auction_id
    FOR UPDATE;
    
    -- Валідація
    IF NOT FOUND THEN
        RETURN QUERY SELECT 0, 'Auction not found'::TEXT;
        RETURN;
    END IF;
    
    IF v_status <> 1 THEN
        RETURN QUERY SELECT 0, 'Auction is not active'::TEXT;
        RETURN;
    END IF;
    
    IF v_end_time < CURRENT_TIMESTAMP THEN
        RETURN QUERY SELECT 0, 'Auction has ended'::TEXT;
        RETURN;
    END IF;
    
    IF p_bid_amount <= v_current_price THEN
        RETURN QUERY SELECT 0, 'Bid must be higher than current price'::TEXT;
        RETURN;
    END IF;
    
    -- Вставляємо ставку
    INSERT INTO bids (auction_id, user_id, bid_amount, timestamp)
    VALUES (p_auction_id, p_user_id, p_bid_amount, CURRENT_TIMESTAMP);
    
    -- Оновлюємо поточну ціну та переможця
    UPDATE auctions
    SET current_price = p_bid_amount,
        winner_user_id = p_user_id
    WHERE auction_id = p_auction_id;
    
    -- Повертаємо успіх
    RETURN QUERY SELECT 1, 'Bid placed successfully'::TEXT;
    
EXCEPTION
    WHEN OTHERS THEN
        RETURN QUERY SELECT 0, SQLERRM::TEXT;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- SP 2: Завершити аукціон та встановити переможця
-- ========================================
CREATE OR REPLACE FUNCTION sp_finalize_auction(
    p_auction_id BIGINT
)
RETURNS TABLE (
    success INT,
    message TEXT,
    winner_user_id BIGINT
) AS $$
DECLARE
    v_status INT;
    v_winner_id BIGINT;
BEGIN
    -- Блокуємо аукціон
    SELECT status, auctions.winner_user_id
    INTO v_status, v_winner_id
    FROM auctions
    WHERE auction_id = p_auction_id
    FOR UPDATE;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 0, 'Auction not found'::TEXT, NULL::BIGINT;
        RETURN;
    END IF;
    
    IF v_status = 2 THEN
        RETURN QUERY SELECT 0, 'Auction already finalized'::TEXT, v_winner_id;
        RETURN;
    END IF;
    
    -- Оновлюємо статус на Finished
    UPDATE auctions
    SET status = 2 -- Finished
    WHERE auction_id = p_auction_id;
    
    RETURN QUERY SELECT 1, 'Auction finalized'::TEXT, v_winner_id;
    
EXCEPTION
    WHEN OTHERS THEN
        RETURN QUERY SELECT 0, SQLERRM::TEXT, NULL::BIGINT;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- SP 3: Створити платіж після завершення аукціону
-- ========================================
CREATE OR REPLACE FUNCTION sp_create_payment(
    p_auction_id BIGINT,
    p_user_id BIGINT,
    p_amount DECIMAL(18, 2)
)
RETURNS TABLE (
    success INT,
    message TEXT,
    payment_id BIGINT
) AS $$
DECLARE
    v_status INT;
    v_existing_payment BIGINT;
    v_new_payment_id BIGINT;
BEGIN
    -- Перевіряємо статус аукціону
    SELECT status
    INTO v_status
    FROM auctions
    WHERE auction_id = p_auction_id
    FOR UPDATE;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 0, 'Auction not found'::TEXT, NULL::BIGINT;
        RETURN;
    END IF;
    
    IF v_status <> 2 THEN
        RETURN QUERY SELECT 0, 'Auction is not finalized'::TEXT, NULL::BIGINT;
        RETURN;
    END IF;
    
    -- Перевіряємо чи платіж вже існує (1:1)
    SELECT payments.payment_id
    INTO v_existing_payment
    FROM payments
    WHERE payments.auction_id = p_auction_id;
    
    IF v_existing_payment IS NOT NULL THEN
        RETURN QUERY SELECT 0, 'Payment already exists for this auction'::TEXT, v_existing_payment;
        RETURN;
    END IF;
    
    -- Створюємо платіж
    INSERT INTO payments (auction_id, user_id, amount, payment_time, transaction_status)
    VALUES (p_auction_id, p_user_id, p_amount, CURRENT_TIMESTAMP, 0) -- 0 = Processing
    RETURNING payments.payment_id INTO v_new_payment_id;
    
    -- Оновлюємо статус аукціону на Paid
    UPDATE auctions
    SET status = 3 -- Paid
    WHERE auction_id = p_auction_id;
    
    RETURN QUERY SELECT 1, 'Payment created'::TEXT, v_new_payment_id;
    
EXCEPTION
    WHEN OTHERS THEN
        RETURN QUERY SELECT 0, SQLERRM::TEXT, NULL::BIGINT;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- SP 4: Отримати всі ставки користувача
-- ========================================
CREATE OR REPLACE FUNCTION sp_get_user_bids(
    p_user_id BIGINT
)
RETURNS TABLE (
    bid_id BIGINT,
    auction_id BIGINT,
    bid_amount DECIMAL(18, 2),
    bid_timestamp TIMESTAMP,
    artwork_name VARCHAR(255),
    auction_status INT,
    current_price DECIMAL(18, 2),
    is_winning INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        b.bid_id,
        b.auction_id,
        b.bid_amount,
        b.timestamp AS bid_timestamp,
        a.artwork_name,
        a.status AS auction_status,
        a.current_price,
        CASE WHEN a.winner_user_id = p_user_id THEN 1 ELSE 0 END AS is_winning
    FROM bids b
    INNER JOIN auctions a ON b.auction_id = a.auction_id
    WHERE b.user_id = p_user_id
    ORDER BY b.timestamp DESC;
END;
$$ LANGUAGE plpgsql;

-- Виведення повідомлення
DO $$
BEGIN
    RAISE NOTICE 'Stored procedures created successfully!';
END $$;
