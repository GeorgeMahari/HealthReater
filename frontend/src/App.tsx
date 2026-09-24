import { Routes, Route } from "react-router-dom";
import { Layout } from "./components/Layout";
import { HomePage } from "./pages/HomePage";
import { AssessmentPage } from "./pages/AssessmentPage";
import { ResultsPage } from "./pages/ResultsPage";
import { AssessmentProvider } from "./context/AssessmentContext";

function App() {
  return (
    <AssessmentProvider>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/assessment" element={<AssessmentPage />} />
          <Route path="/results" element={<ResultsPage />} />
        </Route>
      </Routes>
    </AssessmentProvider>
  );
}

export default App;
