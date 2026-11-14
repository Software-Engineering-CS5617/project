using System.Text.RegularExpressions;

namespace Cloud.Crash;

/// <summary>
/// Turns stack frames into stable names that do not change with line numbers, lambdas or generic arguments.
/// </summary>
public static partial class StackNormalizer
{
    /// <summary>
    /// Normalizes one frame to <c>Namespace.Type.Method</c>.
    /// </summary>
    /// <param name="frame">The frame to normalize.</param>
    /// <returns>The normalized name.</returns>
    public static string NormalizeFrame(CrashFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        string declaringType = GenericArgumentsRegex().Replace(frame.DeclaringType, string.Empty);
        List<string> typeParts = [];
        string? stateMachineMethod = null;

        foreach (string segment in declaringType.Split('+'))
        {
            Match stateMachine = StateMachineRegex().Match(segment);

            if (stateMachine.Success)
            {
                stateMachineMethod = stateMachine.Groups["name"].Value;
            }
            else if (!segment.Contains("<>c", StringComparison.Ordinal))
            {
                typeParts.Add(GenericArityRegex().Replace(segment, string.Empty));
            }
        }

        string method = stateMachineMethod is not null && frame.Method == "MoveNext"
            ? NormalizeMethodName(stateMachineMethod)
            : NormalizeMethodName(frame.Method);

        string typeName = string.Join('.', typeParts);
        return typeName.Length == 0 ? method : typeName + "." + method;
    }

    private static string NormalizeMethodName(string method)
    {
        Match lambda = LambdaRegex().Match(method);

        if (lambda.Success)
        {
            return NormalizeMethodName(lambda.Groups["name"].Value);
        }

        Match localFunction = LocalFunctionRegex().Match(method);

        if (localFunction.Success)
        {
            return NormalizeMethodName(localFunction.Groups["outer"].Value) + "." + localFunction.Groups["local"].Value;
        }

        return GenericArityRegex().Replace(method, string.Empty);
    }

    [GeneratedRegex(@"^<(?<name>.+)>d__\d+$", RegexOptions.CultureInvariant, 200)]
    private static partial Regex StateMachineRegex();

    [GeneratedRegex(@"^<(?<name>.+)>b__\d+(?:_\d+)*$", RegexOptions.CultureInvariant, 200)]
    private static partial Regex LambdaRegex();

    [GeneratedRegex(@"^<(?<outer>.+)>g__(?<local>[^|]+)\|\d+(?:_\d+)*$", RegexOptions.CultureInvariant, 200)]
    private static partial Regex LocalFunctionRegex();

    [GeneratedRegex(@"`\d+", RegexOptions.CultureInvariant, 200)]
    private static partial Regex GenericArityRegex();

    [GeneratedRegex(@"\[.*\]", RegexOptions.CultureInvariant | RegexOptions.Singleline, 200)]
    private static partial Regex GenericArgumentsRegex();
}
