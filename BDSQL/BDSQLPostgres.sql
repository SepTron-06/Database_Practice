

-- Создание таблицы Услуга
CREATE TABLE IF NOT EXISTS Услуга (
    УслугаD INT PRIMARY KEY DEFAULT nextval('seq_услуга'),
    Название_услуги VARCHAR(100) NOT NULL,
    Тип_услуги VARCHAR(50) NOT NULL
);

-- Создание таблицы Клиент
CREATE TABLE IF NOT EXISTS Клиент (
    КлиентD INT PRIMARY KEY DEFAULT nextval('seq_клиент'),
    ФИО VARCHAR(200) NOT NULL,
    Email VARCHAR(100),
    Телефон VARCHAR(20)
);

-- Создание таблицы Адрес
CREATE TABLE IF NOT EXISTS Адрес (
    АдресD INT PRIMARY KEY DEFAULT nextval('seq_адрес'),
    КлиентD INT NOT NULL,
    Улица VARCHAR(150) NOT NULL,
    Дом VARCHAR(10) NOT NULL,
    Квартира VARCHAR(10),
    Площадь_жилья DECIMAL(8,2),
    Количество_проживающих INT,
    FOREIGN KEY (КлиентD) REFERENCES Клиент(КлиентD)
);

-- Создание таблицы Тариф
CREATE TABLE IF NOT EXISTS Тариф (
    ТарифD INT PRIMARY KEY DEFAULT nextval('seq_тариф'),
    УслугаD INT NOT NULL,
    Цена_за_квадратный_метр DECIMAL(10,2),
    Цена_за_человека DECIMAL(10,2),
    Цена_за_потребляемый_объем DECIMAL(10,2),
    FOREIGN KEY (УслугаD) REFERENCES Услуга(УслугаD)
);

-- Создание таблицы Счет
CREATE TABLE IF NOT EXISTS Счет (
    СчетD INT PRIMARY KEY DEFAULT nextval('seq_счет'),
    АдресD INT NOT NULL,
    ТарифD INT NOT NULL,
    Потребляемый_объем DECIMAL(10,2),
    Сумма DECIMAL(12,2),
    Дата_оплаты DATE,
    Наличие_оплаты BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (АдресD) REFERENCES Адрес(АдресD),
    FOREIGN KEY (ТарифD) REFERENCES Тариф(ТарифD)
);


CREATE SEQUENCE IF NOT EXISTS  seq_услуга START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE IF NOT EXISTS seq_клиент START WITH 1 INCREMENT BY 1;
CREATE IF NOT EXISTS SEQUENCE IF NOT EXISTS seq_адрес START WITH 1 INCREMENT BY 1;
CREATE IF NOT EXISTS SEQUENCE IF NOT EXISTS seq_тариф START WITH 1 INCREMENT BY 1;
CREATE IF  SEQUENCE IF NOT EXISTS seq_счет START WITH 1 INCREMENT BY 1;

INSERT INTO Услуга (Название_услуги, Тип_услуги) VALUES
('Подача холодной воды', 'Водоснабжение'),
('Подача горячей воды', 'Водоснабжение'),
('Подача газа в квартиру', 'Газоснабжение'),
('Подача электричества в квартиру', 'Электроснабжение'),
('Взнос в капитальный ремонт дома', 'Капитальный ремонт'),
('Обращение с ТКО', 'Вывоз мусора'),
('Отопление', 'Теплоснабжение'),
('Электронное запирающее устройство', 'Электроснабжение');
