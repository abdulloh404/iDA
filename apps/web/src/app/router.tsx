import { Navigate, createBrowserRouter } from 'react-router-dom';
import type { RouteObject } from 'react-router-dom';
import { AppShell } from '../components/layout/AppShell';
import { RequireAuth } from './RequireAuth';
import { LoginScreen } from '../features/auth/LoginScreen';
import { MASTER_DATA_SCREENS } from '../features/master-data/registry';
import { MasterFormScreen } from '../features/master-data/screens/MasterFormScreen';
import { MasterListScreen } from '../features/master-data/screens/MasterListScreen';
import { SettingsScreen } from '../features/master-data/screens/SettingsScreen';
import { HomeScreen } from '../features/home/HomeScreen';
import { ApprovalListScreen } from '../features/approvals/ApprovalListScreen';
import { ApprovalDetailScreen } from '../features/approvals/ApprovalDetailScreen';
import { InterfaceConfigScreen } from '../features/ingest-config/InterfaceConfigScreen';
import { IngestBatchListScreen } from '../features/ingest/IngestBatchListScreen';
import { IngestBatchDetailScreen } from '../features/ingest/IngestBatchDetailScreen';
import { IngestRunDetailScreen } from '../features/ingest/IngestRunDetailScreen';
import type { AnyScreenDescriptor } from '../features/master-data/descriptor';
function routesFor(descriptor: AnyScreenDescriptor): RouteObject[] {
    if (descriptor.singleton) {
        return [{ path: descriptor.path, element: <SettingsScreen descriptor={descriptor}/> }];
    }
    const List = descriptor.ListScreen ?? MasterListScreen;
    const Form = descriptor.FormScreen ?? MasterFormScreen;
    return [
        { path: descriptor.path, element: <List descriptor={descriptor}/> },
        { path: `${descriptor.path}/new`, element: <Form descriptor={descriptor} mode="create"/> },
        { path: `${descriptor.path}/:id`, element: <Form descriptor={descriptor} mode="view"/> },
        { path: `${descriptor.path}/:id/edit`, element: <Form descriptor={descriptor} mode="edit"/> },
        ...(descriptor.extraRoutes ?? []),
    ];
}
export const router = createBrowserRouter([
    { path: '/login', element: <LoginScreen /> },
    {
        path: '/',
        element: (<RequireAuth>
        <AppShell />
      </RequireAuth>),
        children: [
            { index: true, element: <HomeScreen /> },
            { path: '/approvals/mine', element: <ApprovalListScreen scope="mine"/> },
            { path: '/approvals/pending', element: <ApprovalListScreen scope="pending"/> },
            { path: '/approvals/history', element: <ApprovalListScreen scope="history"/> },
            { path: '/approvals/:id', element: <ApprovalDetailScreen /> },
            { path: '/ingest/config', element: <InterfaceConfigScreen /> },
            { path: '/ingest/batches', element: <IngestBatchListScreen /> },
            { path: '/ingest/batches/:batchId', element: <IngestBatchDetailScreen /> },
            { path: '/ingest/runs/:runId', element: <IngestRunDetailScreen /> },
            ...MASTER_DATA_SCREENS.flatMap(routesFor),
            { path: '*', element: <Navigate to="/" replace/> },
        ],
    },
]);
