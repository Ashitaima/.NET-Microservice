-- ========================================
-- Art Auction Platform Database Schema
-- Project #1: Auction Service (PostgreSQL + ADO.NET/Dapper)
-- ========================================

-- Створюємо базу даних (виконайте окремо в psql або pgAdmin)
-- CREATE DATABASE artauctiondb;

-- Підключіться до бази даних artauctiondb перед виконанням цього скрипту

-- ========================================
-- Створюємо таблицю Користувачів (дубльовані дані)
-- ========================================
CREATE TABLE IF NOT EXISTS users (
    user_id BIGINT PRIMARY KEY,
    user_name VARCHAR(100) NOT NULL,
    balance DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    CONSTRAINT chk_users_balance CHECK (balance >= 0)
);

-- ========================================
-- Створюємо таблицю Аукціонів
-- ========================================
CREATE TABLE IF NOT EXISTS auctions (
    auction_id BIGSERIAL PRIMARY KEY,
    artwork_id BIGINT NOT NULL,
    artwork_name VARCHAR(255) NOT NULL,
    seller_user_id BIGINT NOT NULL,
    start_price DECIMAL(18, 2) NOT NULL,
    current_price DECIMAL(18, 2) NOT NULL,
    start_time TIMESTAMP NOT NULL,
    end_time TIMESTAMP NOT NULL,
    status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Active, 2=Finished, 3=Paid
    winner_user_id BIGINT NULL,
    
    CONSTRAINT fk_auctions_winner_user FOREIGN KEY (winner_user_id) REFERENCES users(user_id),
    CONSTRAINT chk_auctions_start_price CHECK (start_price > 0),
    CONSTRAINT chk_auctions_current_price CHECK (current_price >= start_price),
    CONSTRAINT chk_auctions_time CHECK (end_time > start_time)
);

-- Індекси для Аукціонів
CREATE INDEX IF NOT EXISTS ix_auctions_status ON auctions(status) WHERE status = 1;
CREATE INDEX IF NOT EXISTS ix_auctions_end_time ON auctions(end_time);
CREATE INDEX IF NOT EXISTS ix_auctions_artwork_id ON auctions(artwork_id);

-- ========================================
-- Створюємо таблицю Ставок (1:N з Auctions)
-- ========================================
CREATE TABLE IF NOT EXISTS bids (
    bid_id BIGSERIAL PRIMARY KEY,
    auction_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    bid_amount DECIMAL(18, 2) NOT NULL,
    timestamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_bids_auction FOREIGN KEY (auction_id) REFERENCES auctions(auction_id) ON DELETE CASCADE,
    CONSTRAINT fk_bids_user FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT chk_bids_amount CHECK (bid_amount > 0)
);

-- Індекси для швидкого пошуку всіх ставок на аукціоні
CREATE INDEX IF NOT EXISTS ix_bids_auction_id ON bids(auction_id);
CREATE INDEX IF NOT EXISTS ix_bids_user_id ON bids(user_id);

-- ========================================
-- Створюємо таблицю Платежів (1:1 з Auctions)
-- ========================================
CREATE TABLE IF NOT EXISTS payments (
    payment_id BIGSERIAL PRIMARY KEY,
    auction_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    amount DECIMAL(18, 2) NOT NULL,
    payment_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    transaction_status INT NOT NULL DEFAULT 0, -- 0=Processing, 1=Success, 2=Failed
    
    CONSTRAINT fk_payments_auction FOREIGN KEY (auction_id) REFERENCES auctions(auction_id),
    CONSTRAINT fk_payments_user FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT uq_payments_auction_id UNIQUE (auction_id), -- 1:1 relationship
    CONSTRAINT chk_payments_amount CHECK (amount > 0)
);

-- ========================================
-- Створюємо таблицю Списку спостереження (M:N)
-- ========================================
CREATE TABLE IF NOT EXISTS auction_watchlist (
    user_id BIGINT NOT NULL,
    auction_id BIGINT NOT NULL,
    added_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT pk_auction_watchlist PRIMARY KEY (user_id, auction_id),
    CONSTRAINT fk_watchlist_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT fk_watchlist_auction FOREIGN KEY (auction_id) REFERENCES auctions(auction_id) ON DELETE CASCADE
);

-- Виведення повідомлення
DO $$
BEGIN
    RAISE NOTICE 'Database schema created successfully!';
END $$;
