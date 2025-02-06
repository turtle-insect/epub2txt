using System;

namespace epub2txt
{
	internal class Epub
	{
		private bool CheckFormat(String path)
		{
			var mimetype = System.IO.Path.Combine(path, "mimetype");
			if (!System.IO.File.Exists(mimetype)) return false;
			String buffer = System.IO.File.ReadAllText(mimetype);
			if(buffer != "application/epub+zip") return false;

			var metainf = System.IO.Path.Combine(path, "META-INF");
			if (!System.IO.Directory.Exists(metainf)) return false;

			var container = System.IO.Path.Combine(metainf, "container.xml");
			if (!System.IO.File.Exists(container)) return false;

			return true;
		}

		public List<String> GetXhtmlFiles(String path)
		{
			var files = new List<String>();

			if (CheckFormat(path) == false) return files;

			var containerPath = System.IO.Path.Combine(path, "META-INF\\container.xml");
			if (!System.IO.File.Exists(containerPath)) return files;

			var container = System.Xml.Linq.XDocument.Load(containerPath);
			System.Xml.Linq.XNamespace containerNs = "urn:oasis:names:tc:opendocument:xmlns:container";
			var rootfiles = container.Element(containerNs + "container")?.Elements(containerNs + "rootfiles");
			if (rootfiles == null) return files;

			foreach (var rootfile in rootfiles)
			{
				var opfFileAttr = rootfile.Element(containerNs + "rootfile")?.Attribute("full-path");
				if (opfFileAttr == null) continue;

				// .opfファイルの解析.
				var opfFile = System.IO.Path.Combine(path, opfFileAttr.Value);
				if (!System.IO.File.Exists(opfFile)) continue;

				var opfPath = System.IO.Path.GetDirectoryName(opfFile);
				if (opfPath == null) continue;

				var opf = System.Xml.Linq.XDocument.Load(opfFile);
				System.Xml.Linq.XNamespace opfNs = "http://www.idpf.org/2007/opf";
				var items = opf.Element(opfNs + "package")?.Element(opfNs + "manifest")?.Elements(opfNs + "item");
				if (items == null) continue;

				foreach (var item in items)
				{
					var mediatype = item.Attribute("media-type");
					if (mediatype == null) continue;
					if (mediatype.Value != "application/xhtml+xml") continue;

					// 目次はいらないのでスキップする
					// そのうち目次をファイルの分割に利用しても良いかもしれない
					var property = item.Attribute("properties");
					if (property != null && property.Value == "nav") continue;

					var xhtml = item.Attribute("href");
					if (xhtml == null) continue;
					files.Add(System.IO.Path.Combine(opfPath, xhtml.Value));
				}
			}

			return files;
		}
	}
}
