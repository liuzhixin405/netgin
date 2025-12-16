-- 🎲 抽奖系统数据库初始化脚本 (MySQL)

-- 创建抽奖活动表
CREATE TABLE IF NOT EXISTS LuckyDrawActivities (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(200) NOT NULL COMMENT '活动名称',
    Description TEXT COMMENT '活动描述',
    Prize VARCHAR(500) NOT NULL COMMENT '奖品',
    MaxParticipants INT NOT NULL DEFAULT 100 COMMENT '最大参与人数',
    CurrentParticipants INT NOT NULL DEFAULT 0 COMMENT '当前参与人数',
    WinnerCount INT NOT NULL DEFAULT 1 COMMENT '获奖者数量',
    Status INT NOT NULL DEFAULT 0 COMMENT '状态: 0-未开始, 1-进行中, 2-已完成, 3-已取消',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    DrawTime DATETIME NULL COMMENT '开奖时间',
    INDEX idx_status (Status),
    INDEX idx_created_at (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='抽奖活动表';

-- 创建参与者表
CREATE TABLE IF NOT EXISTS Participants (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ActivityId INT NOT NULL COMMENT '活动ID',
    Name VARCHAR(100) NOT NULL COMMENT '参与者名称',
    Contact VARCHAR(100) NOT NULL COMMENT '联系方式',
    LuckyNumber VARCHAR(20) NOT NULL COMMENT '幸运号码',
    IsWinner TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否中奖',
    JoinedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '参与时间',
    INDEX idx_activity_id (ActivityId),
    INDEX idx_is_winner (IsWinner),
    INDEX idx_lucky_number (LuckyNumber),
    CONSTRAINT fk_activity FOREIGN KEY (ActivityId) REFERENCES LuckyDrawActivities(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='参与者表';

-- 插入示例数据
INSERT INTO LuckyDrawActivities (Name, Description, Prize, MaxParticipants, WinnerCount, Status) VALUES
('🎄 圣诞节抽奖活动', '参与圣诞节抽奖，赢取丰厚大奖！', 'iPhone 15 Pro Max', 1000, 3, 1),
('🧧 新年红包雨', '新年福利，人人有机会！', '888元现金红包', 500, 10, 0);
