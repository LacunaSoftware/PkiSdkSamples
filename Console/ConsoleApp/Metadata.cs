using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ConsoleApp
{
	class Metadata
	{

		public IDictionary<string, string> Dict { get; set; }
		public List<string> Keywords { get; set; }

	}

	class MetadataModel
	{
		public DescriptiveMetadata Descriptive { get; set; }
		public AdministrativeMetadata Administrative { get; set; }

		public Metadata ToEntity()
		{
			return new Metadata()
			{
				Dict = GetDict(),
				Keywords = Descriptive.Keywords
			};
		}

		protected IDictionary<string, string> GetDict()
		{

			var metadata = new Dictionary<string, string>();

			if (Descriptive.Keywords?.Any() == true)
			{
				metadata[MetadataKeys.Subject] = Util.GetSubjectFromKeywords(Descriptive.Keywords);
			}

			if (!string.IsNullOrEmpty(Descriptive.Creator))
			{
				metadata[MetadataKeys.Creator] = Descriptive.Creator;
			}

			metadata[MetadataKeys.DateScanned] = Util.FormatForDocument(
				Administrative.DateScanned, "E. South America Standard Time", "pt-BR"
			);

			if (!string.IsNullOrEmpty(Administrative.LocationScanned))
			{
				metadata[MetadataKeys.LocationScanned] = Administrative.LocationScanned;
			}

			if (!string.IsNullOrEmpty(Administrative.ScanningPersonName))
			{
				metadata[MetadataKeys.ScanningPerson] = $"{Administrative.ScanningPersonName} ({Administrative.ScanningPersonCpf})";
			}

			if (!string.IsNullOrEmpty(Administrative.ScanningEntityName))
			{
				metadata[MetadataKeys.ScanningEntity] = $"{Administrative.ScanningEntityName} ({Administrative.ScanningEntityCnpj})";
			}

			if (!string.IsNullOrEmpty(Descriptive.Title))
			{
				metadata[MetadataKeys.Title] = Descriptive.Title;
			}

			if (!string.IsNullOrEmpty(Descriptive.DocumentType))
			{
				metadata[MetadataKeys.DocumentType] = Descriptive.DocumentType;
			}

			metadata[MetadataKeys.ScannedByDomesticGovernment] = MetadataValues.Get(Administrative.ScannedByDomesticGovernment);

			if (!string.IsNullOrEmpty(Descriptive.Classification))
			{
				metadata[MetadataKeys.Classification] = Descriptive.Classification;
			}

			if (!string.IsNullOrEmpty(Descriptive.DateCreated))
			{
				metadata[MetadataKeys.DateCreated] = Descriptive.DateCreated;
			}

			if (!string.IsNullOrEmpty(Descriptive.LocationCreated))
			{
				metadata[MetadataKeys.LocationCreated] = Descriptive.LocationCreated;
			}

			if (Descriptive.Destination.HasValue)
			{
				metadata[MetadataKeys.Destination] = MetadataValues.Get(Descriptive.Destination.Value);
			}

			if (!string.IsNullOrEmpty(Descriptive.Genre))
			{
				metadata[MetadataKeys.Genre] = Descriptive.Genre;
			}

			if (!string.IsNullOrEmpty(Descriptive.StoragePeriod))
			{
				metadata[MetadataKeys.StoragePeriod] = Descriptive.StoragePeriod;
			}

			return metadata;
		}

	}

	internal class CodeAttribute : Attribute
	{
		public CodeAttribute(string code)
		{
			Code = code;
		}

		public string Code { get; }
	}

	internal enum Destinations
	{

		[Code("T")]
		Transfer = 1,

		[Code("D")]
		Destruction,

		[Code("S")]
		Storage,
	}

	internal static class MetadataKeys
	{
		public const string Subject = "Assunto";
		public const string Creator = "Autor (nome)";
		public const string DateScanned = "Data da digitalizacao";
		public const string LocationScanned = "Local da digitalizacao";
		public const string DocumentId = "Identificador do documento digital";
		public const string ScanningPerson = "Responsavel pela digitalizacao";
		public const string ScanningEntity = "Pessoa juridica responsavel pela digitalizacao";
		public const string Title = "Titulo";
		public const string DocumentType = "Tipo documental";
		public const string Classification = "Classe";
		public const string DateCreated = "Data de producao (do documento original)";
		public const string LocationCreated = "Local de producao (do documento original)";
		public const string Destination = "Destinacao prevista (eliminacao ou guarda permanente)";
		public const string Genre = "Genero";
		public const string StoragePeriod = "Prazo de guarda";
		public const string ScannedByDomesticGovernment = "Digitalizado por pessoa juridica de direito publico interno";
	}

	internal static class MetadataValues
	{
		public static string Get(bool value) => value ? "Sim" : "Não";

		public static string Get(Destinations value) => value switch
		{
			Destinations.Destruction => "Eliminação",
			Destinations.Storage => "Recolhimento",
			Destinations.Transfer => "Transferência",
			_ => throw new NotImplementedException(),
		};
	}

	internal class DescriptiveMetadata
	{

		/// <summary>
		/// Título do documento (elemento de descrição que nomeia o documento), podendo ser formal ou atribuído
		/// </summary>
		[Required, MaxLength(200)]
		public string Title { get; set; }

		/// <summary>
		/// Assunto do documento (palavras-chave que representam o conteúdo do documento)
		/// </summary>
		[Required]
		public List<string> Keywords { get; set; }

		/// <summary>
		/// Nome do autor do documento (pessoa natural ou jurídica que emitiu o documento, também conhecido como "produtor" do documento)
		/// </summary>
		[Required, MaxLength(200)]
		public string Creator { get; set; }

		/// <summary>
		/// Data de produção (do documento original), no formato ISO 8601-1, i.e. 'AAAA-MM-DD', ou 'AAAA-MM' caso o dia não seja conhecido, ou ainda 'AAAA' caso apenas o ano seja conhecido.
		/// </summary>
		[RegularExpression(@"^\d{4}(-\d{2})?(-\d{2})?$")]
		public string DateCreated { get; set; }

		/// <summary>
		/// Registro tópico (local) da produção do documento
		/// </summary>
		[MaxLength(200)]
		public string LocationCreated { get; set; }

		/// <summary>
		/// Classificação do documento (código identificador da classe, subclasse, grupo ou subgrupo do documento com base em um plano de classificação de documentos). Por exemplo,
		/// '03.01.01.12' (no caso de um Termo de Posse de Senador, segundo o plano de classificação do Senado Federal)
		/// </summary>
		[MaxLength(100)]
		public string Classification { get; set; }

		/// <summary>
		/// Tipo documental (tipo de documento, ou seja, a configuração da espécie documental de acordo com a atividade que a gerou). Por exemplo, 'Termo de Posse de Senador'.
		/// </summary>
		[Required, MaxLength(100)]
		public string DocumentType { get; set; }

		/// <summary>
		/// Destinação prevista (indicação da próxima ação de destinação -- transferência, eliminação ou recolhimento -- prevista para o documento, em cumprimento à tabela
		/// de temporalidade e destinação de documentos das atividades-meio e das atividades-fim.
		/// </summary>
		public Destinations? Destination { get; set; }

		/// <summary>
		/// Gênero (indica o gênero documental, ou seja, a configuração da informação no documento de acordo com o sistema de signos utilizado na comunicação do documento). Por
		/// exemplo: 'Textual' (quando a informação está escrita), 'Cartográfico' (quando o documento representa uma área maior, como em plantas e mapas) ou 'Iconográfico'
		/// (quando o documento possui a informação em forma de imagem estática, como em fotografias, partituras, e cartazes).
		/// </summary>
		[MaxLength(50)]
		public string Genre { get; set; }

		/// <summary>
		/// Prazo de guarda (indicação do prazo estabelecido em tabela de temporalidade para o cumprimento da destinação)
		/// </summary>
		[MaxLength(100)]
		public string StoragePeriod { get; set; }
	}
	
	internal class AdministrativeMetadata
	{

		/// <summary>
		/// Denota se o documento foi digitalizado por pessoa jurídica de direito público interno
		/// </summary>
		public bool ScannedByDomesticGovernment { get; set; }

		/// <summary>
		/// Nome completo da pessoa física responsável pela digitalização
		/// </summary>
		public string ScanningPersonName { get; set; }

		/// <summary>
		/// CPF da pessoa física responsável pela digitalização
		/// </summary>
		public string ScanningPersonCpf { get; set; }

		/// <summary>
		/// E-mail da pessoa física responsável pela digitalização
		/// </summary>
		public string ScanningPersonEmail { get; set; }

		/// <summary>
		/// Razão social da pessoa jurídica responsável pela digitalização (se houver)
		/// </summary>
		public string ScanningEntityName { get; set; }

		/// <summary>
		/// CNPJ da pessoa jurídica responsável pela digitalização (se houver uma pessoa juríca responsável pela digitalização)
		/// </summary>
		public string ScanningEntityCnpj { get; set; }

		/// <summary>
		/// Data/hora da digitalização do documento
		/// </summary>
		public DateTimeOffset DateScanned { get; set; }

		/// <summary>
		/// Descrição do local onde o documento foi digitalizado
		/// </summary>
		public string LocationScanned { get; set; }
	}

}