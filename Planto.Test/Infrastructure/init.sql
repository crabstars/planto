CREATE TABLE Customers (
                           customer_id SERIAL PRIMARY KEY,
                           customer_name VARCHAR(100) NOT NULL,
                           email VARCHAR(100) UNIQUE NOT NULL
);

-- Create the Orders table with a foreign key to Customers
CREATE TABLE Orders (
                        order_id SERIAL PRIMARY KEY,
                        order_date DATE NOT NULL,
                        customer_id INT NOT NULL,
                        FOREIGN KEY (customer_id) REFERENCES Customers(customer_id) ON DELETE CASCADE
);

-- Create the Products table with a foreign key to Orders
CREATE TABLE Products (
                          product_id SERIAL PRIMARY KEY,
                          product_name VARCHAR(100) NOT NULL,
                          price DECIMAL(10, 2) NOT NULL,
                          order_id INT NOT NULL,
                          FOREIGN KEY (order_id) REFERENCES Orders(order_id) ON DELETE CASCADE
);

-- Create the Invoices table with a foreign key to Orders
CREATE TABLE Invoices (
                          invoice_id SERIAL PRIMARY KEY,
                          invoice_date DATE NOT NULL,
                          amount DECIMAL(10, 2) NOT NULL,
                          order_id INT NOT NULL,
                          FOREIGN KEY (order_id) REFERENCES Orders(order_id) ON DELETE CASCADE
);
