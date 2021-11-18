﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SheetAnalisy
{
    public class Program
    {
        private static List<string> ExpensivesProducts;
        private static List<string> CheapierGenerics;
        private static List<string> ProductsTypes;
        private static List<string> SpecialProducts;
        private static List<string> SpecialProductsTypes;
        private static List<string> SpecialProductsMaterialList;
        private static Dictionary<string, string> NormalProductsDict;
        private static double ExpensiveProductValue;
        private static double CheapierGenericValue;
        private static double SpecialProductsTotal;
        static async Task Main(string[] args)
        {
            ExpensivesProducts = new List<string>();
            CheapierGenerics = new List<string>();
            ProductsTypes = new List<string>();
            SpecialProducts = new List<string>() { "SUBSTÂNCIA;PRODUTO;PF Sem Impostos;TIPO DE PRODUTO" };
            SpecialProductsTypes = new List<string>();
            ExpensiveProductValue = 0.0;
            SpecialProductsTotal = 0.0;
            CheapierGenericValue = double.PositiveInfinity;

            SpecialProductsMaterialList = new List<string>();
            NormalProductsDict = new Dictionary<string, string>();

            var fileName = @"C:\Files\TA_PRECO_MEDICAMENTO.csv";
            var fileStream = File.OpenRead(fileName);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //var streamReader = File.Open(fileName, FileMode.Open, FileAccess.Read);
            var streamReader = new StreamReader(fileStream, Encoding.GetEncoding(1252), true);
            //var streamReader = new StreamReader(fileStream, Encoding.GetEncoding(1252), true);

            var line = streamReader.ReadLine();
            Print(line, "Fields names");
            line = streamReader.ReadLine();
            int count = 2;
            do
            {
                var fields = Spliter(line);
                if(!(fields is null))
                {
                    var product = fields[8];
                    var valuePF = fields[13];
                    var type = fields[11];
                    var marketable = fields[38];
                    var tarja = fields[39];
                    var material = fields[0];

                    ExpensiveProductManager(valuePF, product);
                    AddNewProductType(type);
                    FindCheapierGeneric(product, type, valuePF);
                    FindSpecialProducts(product, valuePF, marketable, tarja, type, material);
                }else
                    Console.WriteLine($"{count}:{line}");

                count++;
                line = streamReader.ReadLine();
            } while (!string.IsNullOrEmpty(line)/* && count > 0*/);

            Print(ExpensivesProducts, "More expensive products");
            Print(ProductsTypes, "Products types");
            if (CheapierGenerics.Count > 0)
                Print(CheapierGenerics, CheapierGenericValue, "Cheap generics");

            streamReader.Close();
            RegraEspecial();
            await WriteSpecialProductsAsync();
        }

        static void Print(string content, string info)
        {
            Console.WriteLine($"{info}: {content}");
        }
        static void Print(List<string> itemList, string info)
        {
            var content = string.Empty;
            foreach (var item in itemList)
            {
                content += $";{item}";
            }
            content = content.Substring(1);
            Console.WriteLine($"{info}: {content}");
        }
        static void Print(List<string> itemList, double value, string info)
        {
            var content = string.Empty;
            foreach (var item in itemList)
            {
                content += $";{item}";
            }
            content = content.Substring(1);
            Console.WriteLine($"{info}: {content}. RS{value}");
        }

        // Método para fazer o que o string.split de maneira personalizada
        static string[] Spliter(string csvLine)
        {
            var original = csvLine;
            try
            {
                var indexStart = csvLine.IndexOf("\"");
                while (indexStart != -1)
                {
                    var indexEnd = csvLine.IndexOf("\"", indexStart + 1);
                    var stringToReplace = csvLine.Substring(indexStart, indexEnd);
                    if ((indexEnd - indexStart) != (stringToReplace.Length))
                    {
                        var subStart = stringToReplace.IndexOf("\"");
                        var subEnd = stringToReplace.IndexOf("\"", subStart + 1);
                        stringToReplace = stringToReplace.Substring(subStart, subEnd);
                    }
                    var stringReplaced = stringToReplace.Replace(";", ",");
                    csvLine = csvLine.Replace(stringToReplace, stringReplaced);
                    indexStart = csvLine.IndexOf("\"", indexEnd + 1);
                }
                return csvLine.Split(";");

            }
            catch (Exception e)
            {
                Console.WriteLine(original);
                return null;
            }

        }

        //Incluir novo tipo de produto na lista
        static void AddNewProductType(string type)
        {
            if (!ProductsTypes.Contains(type))
                ProductsTypes.Add(type);
        }

        //Incluir novo tipo de produto na lista de produtos que seguem regras especiais
        static void AddNewSpecialProductType(string type)
        {
            if (!SpecialProductsTypes.Contains(type))
                SpecialProductsTypes.Add(type);
        }

        //Busca dos produtos genéricos mais baratos
        static void FindCheapierGeneric(string product, string type, string value)
        {
            
            if (type.ToUpper().Contains("GEN"))
            {
                double newValue = Double.Parse(value);
                if (newValue <= CheapierGenericValue)
                {
                    if (newValue < CheapierGenericValue)
                    {
                        CheapierGenericValue = newValue;
                        CheapierGenerics = new List<string> { product };
                    }
                    else
                    {
                        if(!CheapierGenerics.Contains(product))
                            CheapierGenerics.Add(product);
                    }
                }
            }

        }

        //Busca dos produtos mais caros
        static void ExpensiveProductManager(string value, string product)
        {
            try
            {
                double newValue = Double.Parse(value);
                if (newValue >= ExpensiveProductValue)
                {
                    if (newValue > ExpensiveProductValue)
                    {
                        ExpensiveProductValue = newValue;
                        ExpensivesProducts = new List<string> { product };
                    }
                    else
                    {
                        if (!ExpensivesProducts.Contains(product))
                            ExpensivesProducts.Add(product);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        //Busca dos produtos que seguem regras especiais
        static void FindSpecialProducts(string product, string value, string marketable, string tarja, string type, string material)
        {
            var minimumValue = 100.0;
            double newValue = Double.Parse(value);

            if (newValue >= minimumValue &&
                marketable.ToUpper().Equals("SIM") &&
                tarja.ToUpper().Contains("VERMELHA"))
            {
                SpecialProducts.Add($"{material};{product};{value};{type}");
                SpecialProductsTotal += newValue;
                AddNewSpecialProductType(type);
                if(!SpecialProductsMaterialList.Contains(material))
                    SpecialProductsMaterialList.Add(material);
            }
            else
            {
                NormalProductsDict.TryAdd(material, $"{product}:{newValue}");
            }
        }

        static void RegraEspecial()
        {
            Console.WriteLine(); int count = 0;
            foreach (var material in SpecialProductsMaterialList)
            {
                string value;
                var exist = NormalProductsDict.TryGetValue(material, out value);
                if (exist)
                {
                    var items = value.Split(":");
                    if (Double.Parse(items[1]) < 100.0)
                    {
                        Console.WriteLine($"{material} X {value}");
                        count++;
                    }
                    

                }
            }
            Console.WriteLine(count);

        }

        //Salvar produtos especiais
        static async Task WriteSpecialProductsAsync()
        {
            await File.WriteAllLinesAsync(@"C:\Files\output.csv", SpecialProducts);
        }
    }
}
