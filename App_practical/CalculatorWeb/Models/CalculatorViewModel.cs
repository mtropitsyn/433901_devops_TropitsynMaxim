using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CalculatorWeb.Models;

public sealed class CalculatorViewModel
{
    [Required(ErrorMessage = "Введите первое число.")]
    [StringLength(100, ErrorMessage = "Число слишком длинное.")]
    [Display(Name = "Первое число")]
    public string FirstNumber { get; set; } = "";

    [Required(ErrorMessage = "Введите второе число.")]
    [StringLength(100, ErrorMessage = "Число слишком длинное.")]
    [Display(Name = "Второе число")]
    public string SecondNumber { get; set; } = "";

    [Required(ErrorMessage = "Выберите операцию.")]
    public string Operation { get; set; } = "add";

    [BindNever]
    public string? Result { get; set; }
    [BindNever]
    public string? Expression { get; set; }
}
