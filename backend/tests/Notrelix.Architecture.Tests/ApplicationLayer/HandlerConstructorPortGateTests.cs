namespace Notrelix.Architecture.Tests.ApplicationLayer
{
    using TestHandlers;

    namespace TestHandlers
    {
        public interface IPermissionService { }

        public interface IWorkspacePermissionService { }

        public interface IPermissionEvaluator { }

        public interface IAuthorizationDecisionStore { }

        public interface INonDecisionPort { }

        public sealed class PermissionServiceHandler
        {
            public PermissionServiceHandler(IPermissionService port)
            {
            }
        }

        public sealed class WorkspacePermissionServiceHandler
        {
            public WorkspacePermissionServiceHandler(IWorkspacePermissionService port)
            {
            }
        }

        public sealed class PermissionEvaluatorHandler
        {
            public PermissionEvaluatorHandler(IPermissionEvaluator port)
            {
            }
        }

        public sealed class AuthorizationDecisionStoreHandler
        {
            public AuthorizationDecisionStoreHandler(IAuthorizationDecisionStore port)
            {
            }
        }

        public sealed class SafeHandler
        {
            public SafeHandler(INonDecisionPort port)
            {
            }
        }
    }

    public class HandlerConstructorPortGateTests
    {
        private static readonly string PortNamespace =
            "Notrelix.Architecture.Tests.ApplicationLayer.TestHandlers";

        private static readonly IReadOnlySet<string> ForbiddenPorts = new HashSet<string>(StringComparer.Ordinal)
        {
            $"{PortNamespace}.IPermissionService",
            $"{PortNamespace}.IWorkspacePermissionService",
            $"{PortNamespace}.IPermissionEvaluator",
            $"{PortNamespace}.IAuthorizationDecisionStore",
        };

        private static string FullName(Type type) => type.FullName!;

        private static IReadOnlyList<string> FindForbiddenPorts(params Type[] handlerTypes)
        {
            return HandlerConstructorPortGate.FindForbiddenPorts(handlerTypes, ForbiddenPorts);
        }

        [Fact]
        public void FindForbiddenPorts_Rejects_IPermissionService()
        {
            var violations = FindForbiddenPorts(typeof(PermissionServiceHandler));

            violations.Should().ContainSingle()
                .Which.Should().Be($"{FullName(typeof(PermissionServiceHandler))}:{PortNamespace}.IPermissionService");
        }

        [Fact]
        public void FindForbiddenPorts_Rejects_IWorkspacePermissionService()
        {
            var violations = FindForbiddenPorts(typeof(WorkspacePermissionServiceHandler));

            violations.Should().ContainSingle()
                .Which.Should().Be($"{FullName(typeof(WorkspacePermissionServiceHandler))}:{PortNamespace}.IWorkspacePermissionService");
        }

        [Fact]
        public void FindForbiddenPorts_Rejects_IPermissionEvaluator()
        {
            var violations = FindForbiddenPorts(typeof(PermissionEvaluatorHandler));

            violations.Should().ContainSingle()
                .Which.Should().Be($"{FullName(typeof(PermissionEvaluatorHandler))}:{PortNamespace}.IPermissionEvaluator");
        }

        [Fact]
        public void FindForbiddenPorts_Rejects_IAuthorizationDecisionStore()
        {
            var violations = FindForbiddenPorts(typeof(AuthorizationDecisionStoreHandler));

            violations.Should().ContainSingle()
                .Which.Should().Be($"{FullName(typeof(AuthorizationDecisionStoreHandler))}:{PortNamespace}.IAuthorizationDecisionStore");
        }

        [Fact]
        public void FindForbiddenPorts_Allows_NonDecisionPort()
        {
            var violations = FindForbiddenPorts(typeof(SafeHandler));

            violations.Should().BeEmpty();
        }

        [Fact]
        public void FindForbiddenPorts_ReturnsSortedDistinct()
        {
            var violations = FindForbiddenPorts(
                typeof(AuthorizationDecisionStoreHandler),
                typeof(PermissionServiceHandler),
                typeof(AuthorizationDecisionStoreHandler));

            violations.Should().HaveCount(2);
            violations[0].Should().Be($"{FullName(typeof(AuthorizationDecisionStoreHandler))}:{PortNamespace}.IAuthorizationDecisionStore");
            violations[1].Should().Be($"{FullName(typeof(PermissionServiceHandler))}:{PortNamespace}.IPermissionService");
        }
    }
}
