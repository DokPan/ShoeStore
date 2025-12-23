using Microsoft.EntityFrameworkCore;
using ShoeStoreDb.Data;
using ShoeStoreDb.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ShoeStoreWPF
{
    public partial class MainWindow : Window
    {
        private readonly User _currentUser;
        private readonly ShoeStoreDbContext _context;
        private List<Product> _allProducts;

        public MainWindow(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _context = new ShoeStoreDbContext();

            txtUserInfo.Text = $"{_currentUser.FullName} ({_currentUser.Role?.RoleName})";
            LoadData();
            ApplyUserPermissions();
        }

        private void LoadData()
        {
            var manufacturers = _context.Manufacturers.OrderBy(m => m.ManufacturerName).ToList();
            cmbManufacturer.Items.Clear();
            cmbManufacturer.Items.Add(new ManufacturerViewModel { ManufacturerId = 0, ManufacturerName = "Все производители" });
            foreach (var manufacturer in manufacturers)
            {
                cmbManufacturer.Items.Add(new ManufacturerViewModel
                {
                    ManufacturerId = manufacturer.ManufacturerId,
                    ManufacturerName = manufacturer.ManufacturerName
                });
            }
            cmbManufacturer.DisplayMemberPath = "ManufacturerName";
            cmbManufacturer.SelectedIndex = 0;

            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                _allProducts = _context.Products
                    .Include(p => p.Manufacturer)
                    .Include(p => p.Supplier)
                    .Include(p => p.Category)
                    .ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}");
            }
        }

        private void ApplyUserPermissions()
        {
            bool isManagerOrAdmin = _currentUser.RoleId == 1 || _currentUser.RoleId == 2;
            panelAdminButtons.Visibility = isManagerOrAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplyFilters()
        {
            if (_allProducts == null) return;

            try
            {
                var filtered = _allProducts.AsQueryable();

                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    var search = txtSearch.Text.ToLower();
                    filtered = filtered.Where(p =>
                        p.Name.ToLower().Contains(search) ||
                        (p.Description != null && p.Description.ToLower().Contains(search)));
                }

                if (cmbManufacturer.SelectedItem is ManufacturerViewModel selectedManufacturer && selectedManufacturer.ManufacturerId > 0)
                {
                    filtered = filtered.Where(p => p.ManufacturerId == selectedManufacturer.ManufacturerId);
                }

                if (decimal.TryParse(txtMaxPrice.Text, out decimal maxPrice) && maxPrice > 0)
                {
                    filtered = filtered.Where(p => p.Price <= maxPrice);
                }

                if (chkDiscount.IsChecked == true)
                {
                    filtered = filtered.Where(p => p.Discount > 0);
                }

                if (chkInStock.IsChecked == true)
                {
                    filtered = filtered.Where(p => p.StockQuantity > 0);
                }

                var sortedList = filtered.ToList();
                var selectedItem = cmbSort.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    switch (selectedItem.Content.ToString())
                    {
                        case "По названию (Я-А)":
                            sortedList = sortedList.OrderByDescending(p => p.Name).ToList();
                            break;
                        case "По цене (возрастание)":
                            sortedList = sortedList.OrderBy(p => p.Price).ToList();
                            break;
                        case "По цене (убывание)":
                            sortedList = sortedList.OrderByDescending(p => p.Price).ToList();
                            break;
                        case "По поставщику (А-Я)":
                            sortedList = sortedList.OrderBy(p => p.Supplier?.SupplierName ?? "").ToList();
                            break;
                        case "По поставщику (Я-А)":
                            sortedList = sortedList.OrderByDescending(p => p.Supplier?.SupplierName ?? "").ToList();
                            break;
                        default:
                            sortedList = sortedList.OrderBy(p => p.Name).ToList();
                            break;
                    }
                }

                DisplayProducts(sortedList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}");
            }
        }

        private void DisplayProducts(List<Product> products)
        {
            itemsProducts.Items.Clear();

            foreach (var product in products)
            {
                itemsProducts.Items.Add(new ProductViewModel(product));
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new ProductEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                LoadProducts();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (itemsProducts.SelectedItem is ProductViewModel selectedProduct)
            {
                var product = _context.Products.Find(selectedProduct.ProductId);
                var editWindow = new ProductEditWindow(product);
                if (editWindow.ShowDialog() == true)
                {
                    LoadProducts();
                }
            }
            else
            {
                MessageBox.Show("Выберите товар для редактирования");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (itemsProducts.SelectedItem is ProductViewModel selectedProduct)
            {
                var result = MessageBox.Show("Удалить товар?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var product = _context.Products.Find(selectedProduct.ProductId);
                    _context.Products.Remove(product);
                    _context.SaveChanges();
                    LoadProducts();
                }
            }
            else
            {
                MessageBox.Show("Выберите товар для удаления");
            }
        }

        private void btnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int productId)
            {
                var product = _allProducts.FirstOrDefault(p => p.ProductId == productId);
                if (product != null)
                {
                    MessageBox.Show($"Товар \"{product.Name}\" добавлен в корзину!");
                }
            }
        }
    }

    public class ManufacturerViewModel
    {
        public int ManufacturerId { get; set; }
        public string ManufacturerName { get; set; }
    }

    public class ProductViewModel
    {
        private readonly Product _product;
        private BitmapImage _cachedImage;

        public ProductViewModel(Product product)
        {
            _product = product;
        }

        public int ProductId => _product.ProductId;
        public string Name => _product.Name;
        public string Article => _product.Article;
        public string Description => _product.Description ?? "Нет описания";
        public decimal Price => _product.Price;
        public decimal Discount => _product.Discount;
        public int StockQuantity => _product.StockQuantity;
        public string Unit => _product.Unit ?? "шт.";
        public string ManufacturerName => _product.Manufacturer?.ManufacturerName ?? "Не указан";
        public bool HasDiscount => _product.Discount > 0;
        public decimal FinalPrice => _product.Price * (1 - _product.Discount / 100m);
        public string DiscountBackground => _product.Discount > 0 ? "#00FA9A" : "Transparent";

        public BitmapImage ProductImage
        {
            get
            {
                if (_cachedImage != null) return _cachedImage;

                if (_product.ImageData != null && _product.ImageData.Length > 0)
                {
                    try
                    {
                        _cachedImage = new BitmapImage();
                        using (var stream = new MemoryStream(_product.ImageData))
                        {
                            _cachedImage.BeginInit();
                            _cachedImage.CacheOption = BitmapCacheOption.OnLoad;
                            _cachedImage.StreamSource = stream;
                            _cachedImage.EndInit();
                            _cachedImage.Freeze();
                        }
                        return _cachedImage;
                    }
                    catch
                    {
                        return LoadDefaultImage();
                    }
                }

                return LoadDefaultImage();
            }
        }

        private BitmapImage LoadDefaultImage()
        {
            try
            {
                var defaultImage = new BitmapImage();
                defaultImage.BeginInit();
                defaultImage.UriSource = new Uri("pack://application:,,,/Resources/picture.png", UriKind.Absolute);
                defaultImage.CacheOption = BitmapCacheOption.OnLoad;
                defaultImage.EndInit();
                defaultImage.Freeze();
                return defaultImage;
            }
            catch
            {
                return new BitmapImage();
            }
        }
    }
}