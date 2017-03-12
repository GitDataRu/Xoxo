﻿//
//  This file is part of Diwen.Xbrl.
//
//  Author:
//       John Nordberg <john.nordberg@gmail.com>
//
//  Copyright (c) 2015-2016 John Nordberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace Diwen.Xbrl
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public static class InstanceComparer
	{
		public static ComparisonReport Report(string a, string b)
		{
			return Report(Instance.FromFile(a), Instance.FromFile(b), ComparisonTypes.All);
		}

		public static ComparisonReport Report(string a, string b, ComparisonTypes comparisonTypes)
		{
			return Report(Instance.FromFile(a), Instance.FromFile(b), comparisonTypes);
		}

		public static ComparisonReport Report(Stream a, Stream b)
		{
			return Report(Instance.FromStream(a), Instance.FromStream(b), ComparisonTypes.All);
		}

		public static ComparisonReport Report(Stream a, Stream b, ComparisonTypes comparisonTypes)
		{
			return Report(Instance.FromStream(a), Instance.FromStream(b), comparisonTypes);
		}

		public static ComparisonReport Report(Instance a, Instance b)
		{
			return Report(a, b, ComparisonTypes.All);
		}

		public static ComparisonReport Report(Instance a, Instance b, ComparisonTypes comparisonTypes)
		{
			return Report(a, b, comparisonTypes, BasicComparisons.All);
		}

		public static ComparisonReport Report(Instance a, Instance b, ComparisonTypes comparisonTypes, BasicComparisons basicComparisons)
		{
			var messages = new List<string>();

			if (comparisonTypes.HasFlag(ComparisonTypes.Basic))
			{
				messages.AddRange(BasicComparison(a, b, basicComparisons));
			}

			messages.AddRange(ComparisonMethods.
							Where(c => comparisonTypes.HasFlag(c.Key)).
							  SelectMany(c => c.Value(a, b)));

			return new ComparisonReport(!messages.Any(), messages);
		}

		static Dictionary<ComparisonTypes, Func<Instance, Instance, IEnumerable<string>>> ComparisonMethods
		= new Dictionary<ComparisonTypes, Func<Instance, Instance, IEnumerable<string>>> {
			{ ComparisonTypes.Contexts, ScenarioComparisonMessages },
			{ ComparisonTypes.Facts, FactComparisonMessages },
			{ ComparisonTypes.DomainNamespaces, DomainNamespaceComparisonMessages },
			{ ComparisonTypes.Units, UnitComparisonMessages },
			{ ComparisonTypes.Entity, EntityComparisonMessages },
			{ ComparisonTypes.Period, PeriodComparisonMessages },
			{ ComparisonTypes.TaxonomyVersion, TaxonomyVersionComparisonMessages },
			{ ComparisonTypes.SchemaReference, SchemaReferenceComparisonMessages },
			{ ComparisonTypes.FilingIndicators, FilingIndicatorComparisonMessages }
		};

		#region SimpleChecks

		static Dictionary<BasicComparisons, Tuple<string, Func<Instance, Instance, bool>>> SimpleCheckMethods
		= new Dictionary<BasicComparisons, Tuple<string, Func<Instance, Instance, bool>>> {
			{BasicComparisons.NullInstances, Tuple.Create<string, Func<Instance, Instance, bool>>("At least one the instances is null", CheckNullInstances) },
			{BasicComparisons.SchemaReference,Tuple.Create<string, Func<Instance, Instance, bool>>( "Different SchemaReference", CheckSchemaReference) },
			{BasicComparisons.Units,Tuple.Create<string, Func<Instance, Instance, bool>>( "Different Units", CheckUnits) },
			{BasicComparisons.FilingIndicators,Tuple.Create<string, Func<Instance, Instance, bool>>( "Different FilingIndicators", CheckFilingIndicators) },
			{BasicComparisons.ContextCount,Tuple.Create<string, Func<Instance, Instance, bool>>( "Different number of Contexts", CheckContextCount) },
			{BasicComparisons.FactCount,Tuple.Create<string, Func<Instance, Instance, bool>>( "Different number of Facts", CheckFactCount) },
			{BasicComparisons.DomainNamespaces,Tuple.Create<string, Func<Instance, Instance, bool>>( "Different domain namespaces", CheckDomainNamespaces) },
			{BasicComparisons.Entity,Tuple.Create<string, Func<Instance, Instance, bool>>( "Different Entity", CheckEntity) },
			{BasicComparisons.Period,Tuple.Create<string, Func<Instance, Instance, bool>>( "Different Period", CheckPeriod) }
		};

		static IEnumerable<string> BasicComparison(Instance a, Instance b, BasicComparisons selection)
		{
			return SimpleCheckMethods.
 				Where(c => selection.HasFlag(c.Key)).
				Where(c => !c.Value.Item2(a, b)).
				Select(c => c.Value.Item1);
		}

		static bool CheckNullInstances(object a, object b)
		{
			return (a != null && b != null);
		}

		static bool CheckTaxonomyVersion(Instance a, Instance b)
		{
			return a.TaxonomyVersion != null && b.TaxonomyVersion != null
				? a.TaxonomyVersion.Equals(b.TaxonomyVersion, StringComparison.Ordinal)
				: a.TaxonomyVersion == null && b.TaxonomyVersion == null;
		}

		static bool CheckSchemaReference(Instance a, Instance b)
		{
			return a.SchemaReference.Equals(b.SchemaReference);
		}

		static bool CheckUnits(Instance a, Instance b)
		{
			return a.Units.Equals(b.Units);
		}

		static bool CheckFilingIndicators(Instance a, Instance b)
		{
			return a.FilingIndicators.Equals(b.FilingIndicators);
		}

		static bool CheckCount<T>(ICollection<T> a, ICollection<T> b)
		{
			return a != null && b != null
				? a.Count == b.Count
				: a == null && b == null;
		}

		static bool CheckContextCount(Instance a, Instance b)
		{
			return CheckCount(a.Contexts, b.Contexts);
		}

		static bool CheckFactCount(Instance a, Instance b)
		{
			return CheckCount(a.Facts, b.Facts);
		}

		static bool CheckDomainNamespaces(Instance a, Instance b)
		{
			return a.GetUsedDomainNamespaces().
				ContentCompare(b.GetUsedDomainNamespaces());
		}

		static bool CheckEntity(Instance a, Instance b)
		{
			Entity entityA = null;
			Entity entityB = null;
			if (a.Contexts != null && a.Contexts.Any())
			{
				entityA = a.Contexts.First().Entity;

				if (b.Contexts != null && b.Contexts.Any())
				{
					entityB = b.Contexts.First().Entity;
				}
			}

			return (entityA == null && entityB == null)
			|| (entityA != null && entityA.Equals(entityB));
		}

		static bool CheckPeriod(Instance a, Instance b)
		{
			Period periodA = null;
			Period periodB = null;
			if (a.Contexts != null && a.Contexts.Any())
			{
				periodA = a.Contexts.First().Period;

				if (b.Contexts != null && b.Contexts.Any())
				{
					periodB = b.Contexts.First().Period;
				}
			}

			return (periodA == null && periodB == null)
			|| (periodA != null && periodA.Equals(periodB));
		}

		#endregion

		#region DetailedChecks

		static Tuple<List<Context>, List<Context>> ContextComparison(Instance a, Instance b)
		{
			return a.Contexts.ContentCompareReport(b.Contexts);
		}

		static IEnumerable<string> ContextComparisonMessages(Instance a, Instance b)
		{
			var differences = ContextComparison(a, b);
			var messages = new List<string>(differences.Item1.Count + differences.Item2.Count);
			messages.AddRange(differences.Item1.Select(item => "(a) " + item.Id + ":" + (item.Scenario != null ? item.Scenario.ToString() : string.Empty)));
			messages.AddRange(differences.Item2.Select(item => "(b) " + item.Id + ":" + (item.Scenario != null ? item.Scenario.ToString() : string.Empty)));
			return messages;
		}

		static Tuple<List<Scenario>, List<Scenario>> ScenarioComparison(Instance a, Instance b)
		{
			var aList = a.
			 Contexts.
			 Select(c => c.Scenario).
			 ToList();

			var bList = b.
						 Contexts.
						 Select(c => c.Scenario).
						 ToList();

			var differences = aList.ContentCompareReport(bList);

			return differences;
		}

		static IEnumerable<string> ScenarioComparisonMessages(Instance a, Instance b)
		{
			var differences = ScenarioComparison(a, b);
			var notInB = differences.Item1;
			var notInA = differences.Item2;

			var messages = new List<string>(notInB.Count + notInA.Count);

			if (notInB.Any())
			{
				// not until we're sure that there won't be duplicates
				// var aLookup = a.Contexts.ToDictionary(c => c.Scenario != null ? c.Scenario.ToString() : "", c => c.Id);
				var aLookup = new Dictionary<string, string>();
				foreach (var c in a.Contexts)
				{
					string key = c.Scenario != null ? c.Scenario.ToString() : "";
					aLookup[key] = c.Id;
				}

				foreach (var item in notInB)
				{
					var key = item != null ? item.ToString() : "";
					var contextId = aLookup[key];
					messages.Add($"(a) {contextId}: {item}");
				}
			}

			if (notInA.Any())
			{
				// not until we're sure that there won't be duplicates
				// var bLookup = b.Contexts.ToDictionary(c => c.Scenario != null ? c.Scenario.ToString() : "", c => c.Id);
				var bLookup = new Dictionary<string, string>();
				foreach (var c in b.Contexts)
				{
					string key = c.Scenario != null ? c.Scenario.ToString() : "";
					bLookup[key] = c.Id;
				}

				foreach (var item in notInA)
				{
					var key = item != null ? item.ToString() : "";
					var contextId = bLookup[key];
					messages.Add($"(b) {contextId}: {item}");
				}
			}
			return messages;
		}

		static Tuple<List<Fact>, List<Fact>> FactComparison(Instance a, Instance b)
		{
			return a.Facts.ContentCompareReport(b.Facts);
		}

		static IEnumerable<string> FactComparisonMessages(Instance a, Instance b)
		{
			var differences = FactComparison(a, b);
			var result = new List<string>(differences.Item1.Count + differences.Item2.Count);
			result.AddRange(differences.Item1.Select(item => $"(a) {item} ({item.Context.Scenario})"));
			result.AddRange(differences.Item2.Select(item => $"(b) {item} ({item.Context.Scenario})"));
			return result;
		}

		static Tuple<List<string>, List<string>> DomainNamespaceComparison(Instance a, Instance b)
		{
			return a.GetUsedDomainNamespaces().
				ContentCompareReport(b.GetUsedDomainNamespaces());
		}

		static IEnumerable<string> DomainNamespaceComparisonMessages(Instance a, Instance b)
		{
			var differences = DomainNamespaceComparison(a, b);
			var result = new List<string>(differences.Item1.Count + differences.Item2.Count);
			result.AddRange(differences.Item1.Select(item => $"(a) {item}"));
			result.AddRange(differences.Item2.Select(item => $"(b) {item}"));
			return result;
		}

		static Tuple<List<Unit>, List<Unit>> UnitComparison(Instance a, Instance b)
		{
			return a.Units.
					ContentCompareReport(b.Units);
		}

		static IEnumerable<string> UnitComparisonMessages(Instance a, Instance b)
		{
			var differences = a.Units.
				ContentCompareReport(b.Units);

			return differences.Item1.Select(item => $"(a) {item}").
							  Concat(differences.Item2.Select(item => $"(b) {item}")).
				OrderBy(m => m);
		}

		static Tuple<List<Identifier>, List<Identifier>> EntityComparison(Instance a, Instance b)
		{
			var aList = new List<Identifier>();
			var bList = new List<Identifier>();

			if (a.Contexts != null && a.Contexts.Any())
			{
				aList.Add(a.Contexts.First().Entity.Identifier);
			}

			if (b.Contexts != null && b.Contexts.Any())
			{
				bList.Add(b.Contexts.First().Entity.Identifier);
			}

			return aList.ContentCompareReport(bList);
		}

		static IEnumerable<string> EntityComparisonMessages(Instance a, Instance b)
		{
			var differences = EntityComparison(a, b);
			return differences.Item1.Select(item => $"(a) Identifier={item}").
			Concat(differences.Item2.Select(item => $"(b) Identifier={item}"));
		}

		static Tuple<List<Period>, List<Period>> PeriodComparison(Instance a, Instance b)
		{
			var aList = new List<Period>();
			var bList = new List<Period>();

			if (a.Contexts != null && a.Contexts.Any())
			{
				aList.Add(a.Contexts.First().Period);
			}

			if (b.Contexts != null && b.Contexts.Any())
			{
				bList.Add(b.Contexts.First().Period);
			}

			return aList.ContentCompareReport(bList);
		}

		static IEnumerable<string> PeriodComparisonMessages(Instance a, Instance b)
		{
			var differences = PeriodComparison(a, b);
			return differences.Item1.Select(item => $"(a) {item}").
			Concat(differences.Item2.Select(item => $"(b) {item}"));
		}

		static Tuple<List<string>, List<string>> TaxonomyVersionComparison(Instance a, Instance b)
		{
			var aList = new List<string>();
			var bList = new List<string>();

			aList.Add(a.TaxonomyVersion);
			bList.Add(b.TaxonomyVersion);

			return aList.ContentCompareReport(bList);
		}

		static IEnumerable<string> TaxonomyVersionComparisonMessages(Instance a, Instance b)
		{
			var differences = TaxonomyVersionComparison(a, b);
			return differences.Item1.Select(item => $"(a) taxonomy-version: {item}").
			Concat(differences.Item2.Select(item => $"(b) taxonomy-version: {item}"));
		}

		static Tuple<List<SchemaReference>,List<SchemaReference>> SchemaReferenceComparison(Instance a, Instance b)
		{
			var aList = new List<SchemaReference>();
			var bList = new List<SchemaReference>();

			aList.Add(a.SchemaReference);
			bList.Add(b.SchemaReference);

			return aList.ContentCompareReport(bList);
		}

		static IEnumerable<string> SchemaReferenceComparisonMessages(Instance a, Instance b)
		{
			var differences = SchemaReferenceComparison(a, b);
			return differences.Item1.Select(item => $"(a) {item}").
			Concat(differences.Item2.Select(item => $"(b) {item}"));
		}

		static Tuple<List<FilingIndicator>,List<FilingIndicator>> FilingIndicatorComparison(Instance a, Instance b)
		{
			return a.FilingIndicators.Where(fi => fi.Filed).ToList().
			ContentCompareReport(b.FilingIndicators.Where(fi => fi.Filed).ToList());
		}

		static IEnumerable<string> FilingIndicatorComparisonMessages(Instance a, Instance b)
		{
			var differences = FilingIndicatorComparison(a, b);

			var result = new List<string>(differences.Item1.Count + differences.Item2.Count);
			result.AddRange(differences.Item1.Select(item => $"(a) {item}"));
			result.AddRange(differences.Item2.Select(item => $"(b) {item}"));
			return result;
		}

		#endregion
	}
}

