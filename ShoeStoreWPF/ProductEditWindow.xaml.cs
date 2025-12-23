using Microsoft.EntityFrameworkCore;
using ShoeStoreDb.Data;
using ShoeStoreDb.Models;
using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace ShoeStoreWPF
{
    public partial class ProductEditWindow : Window
    {
        private readonly Product _product;
        private readonly ShoeStoreDbContext _context;
        private string _imagePath;

        public ProductEditWindow(Product product = null)
        {
            InitializeComponent();
            _context = new ShoeStoreDbContext();
            _product = product ?? new Product();

            if (product != null)
            {
                LoadProductData();
            }
        }

        private void LoadProductData()
        {
            txtName.Text = _product.Name;
            txtArticle.Text = _product.Article;
            txtDescription.Text = _product.Description;
            txtPrice.Text = _product.Price.ToString();
            txtDiscount.Text = _product.Discount.ToString();
            txtStockQuantity.Text = _product.StockQuantity.ToString();
        }

        private void btnSelectImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";

            if (dialog.ShowDialog() == true)
            {
                _imagePath = dialog.FileName;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                _product.Name = txtName.Text;
                _product.Article = txtArticle.Text;
                _product.Description = txtDescription.Text;
                _product.Price = decimal.Parse(txtPrice.Text);
                _product.Discount = decimal.Parse(txtDiscount.Text);
                _product.StockQuantity = int.Parse(txtStockQuantity.Text);

                if (!string.IsNullOrEmpty(_imagePath))
                {
                    var imageBytes = File.ReadAllBytes(_imagePath);
                    _product.ImageData = imageBytes;
                }

                if (_product.ProductId == 0)
                {
                    _context.Products.Add(_product);
                }
                else
                {
                    _context.Entry(_product).State = EntityState.Modified;
                }

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ShowError("Введите название товара");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtArticle.Text))
            {
                ShowError("Введите артикул товара");
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
            {
                ShowError("Введите корректную цену");
                return false;
            }

            if (!decimal.TryParse(txtDiscount.Text, out decimal discount) || discount < 0 || discount > 100)
            {
                ShowError("Скидка должна быть от 0 до 100%");
                return false;
            }

            if (!int.TryParse(txtStockQuantity.Text, out int quantity) || quantity < 0)
            {
                ShowError("Введите корректное количество");
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}