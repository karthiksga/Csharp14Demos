using System;
using System.Collections.Generic;
using System.Linq;
using FunctionalExtensions;
using FunctionalExtensions.Optics;
using FunctionalExtensions.ValidationDsl;
using ValidatorApi = FunctionalExtensions.ValidationDsl.ValidatorExtensions;

namespace FunctionalExtensions.Tests;

public class ValidationAndOpticsTests
{
    private sealed record Address(string City, string Street);
    private sealed record Person(string Name, Address Address, int Age);

    [Fact]
    public void UnitAndValidationFactoriesWork()
    {
        Assert.Equal("()", Unit.Value.ToString());

        var success = Validation<int>.Success(5);
        Assert.True(success.IsValid);
        Assert.Equal("Valid(5)", success.ToString());

        var failure = Validation<int>.Failure("oops");
        Assert.False(failure.IsValid);
        Assert.Equal("Invalid([oops])", failure.ToString());

        var viaStatic = Validation.From(10);
        Assert.True(viaStatic.IsValid);
        var viaErrors = Validation.From(10, "missing");
        Assert.False(viaErrors.IsValid);
        Assert.Single(viaErrors.Errors);
    }

    [Fact]
    public void ValidationExtensionsCoverBranches()
    {
        var validation = Validation<int>.Success(5);
        var failed = Validation<int>.Failure("bad");

        Assert.Equal(10, validation.Map(x => x * 2).Value);
        Assert.False(failed.Map(x => x).IsValid);

        var applicative = Validation<Func<int, int>>.Success(x => x + 1);
        Assert.Equal(6, validation.Apply(applicative).Value);

        var applicativeFailure = Validation<Func<int, int>>.Failure("broken");
        var combinedFailure = validation.Apply(applicativeFailure);
        Assert.False(combinedFailure.IsValid);
        Assert.Contains("broken", combinedFailure.Errors);

        var combo = validation.Combine(Validation<int>.Success(2), (l, r) => l + r);
        Assert.Equal(7, combo.Value);

        var comboFailure = Validation<int>.Failure("left").Combine(Validation<int>.Failure(), (l, r) => l + r);
        Assert.False(comboFailure.IsValid);
        Assert.Contains("left", comboFailure.Errors);

        var emptyLogs = Validation<int>.Failure().Combine(Validation<int>.Failure(), (l, r) => l);
        Assert.False(emptyLogs.IsValid);
        Assert.Empty(emptyLogs.Errors);

        Assert.Equal(Validation<int>.Success(5), validation.Select(x => x));

        var ensured = validation.Ensure(x => x > 0, "must be positive");
        Assert.True(ensured.IsValid);
        var ensuredFailure = validation.Ensure(_ => false, () => "neg");
        Assert.False(ensuredFailure.IsValid);
        Assert.Contains("neg", ensuredFailure.Errors);

        Assert.True(validation.ToOption().HasValue);
        Assert.True(validation.ToResult().IsSuccess);
        Assert.True(validation.ToTaskResult(errorMessage: null).Invoke().Result.IsSuccess);
        var failureTaskResult = Validation<int>.Failure("oops").ToTaskResult(errorMessage: null).Invoke().Result;
        Assert.Equal("oops", failureTaskResult.Error);
    }

    [Fact]
    public void ValidatorAccumulatesErrorsAndReusesListPool()
    {
        var validator = Validator<Person>.Empty;
        validator = EnsureRule(validator, p => !string.IsNullOrWhiteSpace(p.Name), "Name required");

        var ageLens = Lens.Create<Person, int>(p => p.Age, (p, age) => p with { Age = age }, "Age");
        validator = EnsureLens(validator, ageLens, age => age >= 18, "Must be adult");

        var addressLens = Lens.Create<Person, Address>(p => p.Address, (p, address) => p with { Address = address }, "Address");
        var addressValidator = EnsureRule(Validator<Address>.Empty, a => a.City.Length > 0, "City");
        validator = EnsureNested(validator, addressLens, addressValidator);

        var invalidPerson = new Person("", new Address("", "Street"), 15);

        var first = validator.Validate(invalidPerson);
        Assert.False(first.IsValid);
        Assert.Equal(3, first.Errors.Count);
        Assert.Contains("Name required", first.Errors);
        Assert.Contains("Age: Must be adult", first.Errors);
        Assert.Contains("Address: City", first.Errors);

        var second = validator.Apply(invalidPerson);
        Assert.False(second.IsValid);

        var valid = validator.Validate(new Person("Ada", new Address("NYC", "Main"), 30));
        Assert.True(valid.IsValid);

        var appended = validator.Append(Validator<Person>.Empty);
        Assert.Same(validator, appended);
    }

    [Fact]
    public void LensHelpersCoverAllPaths()
    {
        var lens = Lens.Create<Person, string>(p => p.Name, (p, name) => p with { Name = name }, "Name");
        var addressLens = Lens.From<Person, string>(p => p.Address.City, (p, city) => p with { Address = p.Address with { City = city } });
        var person = new Person("Dana", new Address("Paris", "Rue"), 25);

        Assert.Equal("Dana", lens.Get(person));
        var renamed = lens.Set(person, "Eve");
        Assert.Equal("Eve", renamed.Name);

        var upper = lens.Over(person, value => value.ToUpperInvariant());
        Assert.Equal("DANA", upper.Name);

        var composed = lens.Compose(Lens.Create<string, char>(s => s[0], (s, c) => c + s[1..], "Initial"));
        Assert.Equal('D', composed.Get(person));
        Assert.Equal("Xana", composed.Set(person, 'X').Name);
        Assert.Equal("Name.Initial", composed.Describe());

        var described = addressLens.Describe();
        Assert.Equal("Address.City", described);

        var lensWithoutPath = Lens.Create<Person, Person>(x => x, (_, v) => v, null);
        Assert.Equal("Lens(Person->Person)", lensWithoutPath.ToString());
        Assert.Equal("Person.Person", lensWithoutPath.Describe());
        Assert.Equal("Lens($)", Lens.Identity<Person>().ToString());

        var unnamedParent = Lens.Create<Person, string>(p => p.Name, (p, name) => p with { Name = name }, null);
        var childWithPath = Lens.Create<string, int>(value => value.Length, (value, length) => value[..length], "Length");
        Assert.Equal("Length", unnamedParent.Compose(childWithPath).Describe());

        var childWithoutPath = Lens.Create<string, char>(s => s[0], (s, c) => c + s[1..], null);
        Assert.Equal("Name", lens.Compose(childWithoutPath).Describe());

        var uninitialized = default(Lens<Person, string>);
        Assert.Throws<InvalidOperationException>(() => uninitialized.Get(person));

        var invalidNext = default(Lens<string, int>);
        Assert.Throws<ArgumentException>(() => lens.Compose(invalidNext));
    }

    [Fact]
    public void ValidatorAppendMergesRules()
    {
        var first = EnsureRule(Validator<Person>.Empty, p => p.Name.Length > 0, "Name");
        var second = EnsureRule(Validator<Person>.Empty, p => p.Age > 0, "Age");

        var combined = first.Append(second);
        var person = new Person("", new Address("", ""), -1);
        var result = combined.Validate(person);

        Assert.False(result.IsValid);
        Assert.Contains("Name", result.Errors);
        Assert.Contains("Age", result.Errors);

        Assert.Same(first, first.Append(Validator<Person>.Empty));
        Assert.Same(first, Validator<Person>.Empty.Append(first));

        var success = Validator<Person>.Empty.Validate(person);
        Assert.True(success.IsValid);
    }

    [Fact]
    public void LensPrefixErrorsStacksPaths()
    {
        var addressLens = Lens.Create<Person, Address>(p => p.Address, (p, a) => p with { Address = a }, "Address");
        var streetLens = Lens.Create<Address, string>(a => a.Street, (a, s) => a with { Street = s }, "Street");
        var validator = EnsureRule(Validator<string>.Empty, street => street.Length > 0, "Street missing");

        var personValidator = EnsureNested(Validator<Person>.Empty, addressLens.Compose(streetLens), validator);
        var result = personValidator.Validate(new Person("Name", new Address("City", ""), 20));

        Assert.False(result.IsValid);
        Assert.Contains("Address.Street: Street missing", result.Errors);
    }

    private static Validator<TSubject> EnsureRule<TSubject>(Validator<TSubject> validator, Func<TSubject, bool> predicate, string error)
        => ValidatorApi.Ensure(validator, predicate, error);

    private static Validator<TSubject> EnsureLens<TSubject, TValue>(Validator<TSubject> validator, Lens<TSubject, TValue> lens, Func<TValue, bool> predicate, string error)
        => ValidatorApi.Ensure(validator, lens, predicate, error);

    private static Validator<TSubject> EnsureNested<TSubject, TValue>(Validator<TSubject> validator, Lens<TSubject, TValue> lens, Validator<TValue> nested)
        => ValidatorApi.Ensure(validator, lens, nested);
}
