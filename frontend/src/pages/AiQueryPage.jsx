import { useState } from 'react';
import { executeAiQuery } from '../api';

function AiQueryPage() {
  const [question, setQuestion] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  const exampleQuestions = [
    'Show all active squads',
    'Which squads have projects in December 2024?',
    'List all team members',
    'What projects are going live this month?',
    'Show me squad capacities',
  ];

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!question.trim()) return;

    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const response = await executeAiQuery(question);
      setResult(response.data);
    } catch (err) {
      setError('Failed to execute query. Please try again.');
      console.error('AI Query error:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleExampleClick = (exampleQuestion) => {
    setQuestion(exampleQuestion);
  };

  return (
    <div className="page-container">
      <div className="page-header">
        <div>
          <h1>Ask Nexus</h1>
          <p style={{ margin: '0.5rem 0 0 0', color: '#666', fontSize: '0.95rem' }}>
            Ask questions about your project data in natural language
          </p>
        </div>
      </div>

      <div className="main-content">
        <div style={{ maxWidth: '1200px', margin: '0 auto' }}>
          {/* Search Form */}
          <div style={{
            background: 'white',
            padding: '2rem',
            borderRadius: '8px',
            boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
            marginBottom: '1.5rem'
          }}>
            <form onSubmit={handleSubmit}>
              <div style={{ display: 'flex', gap: '0.75rem' }}>
                <input
                  type="text"
                  value={question}
                  onChange={(e) => setQuestion(e.target.value)}
                  placeholder="e.g., Which squads have availability next week?"
                  style={{
                    flex: 1,
                    padding: '0.75rem 1rem',
                    border: '1px solid #ddd',
                    borderRadius: '6px',
                    fontSize: '0.95rem'
                  }}
                  disabled={loading}
                />
                <button
                  type="submit"
                  disabled={loading || !question.trim()}
                  className="btn btn-primary"
                  style={{ minWidth: '120px' }}
                >
                  {loading ? (
                    <span style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', justifyContent: 'center' }}>
                      <svg style={{ animation: 'spin 1s linear infinite', height: '18px', width: '18px' }} xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle style={{ opacity: 0.25 }} cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                        <path style={{ opacity: 0.75 }} fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      Thinking...
                    </span>
                  ) : (
                    '🔍 Search'
                  )}
                </button>
              </div>
            </form>

            {/* Example Questions */}
            <div style={{ marginTop: '1.5rem', paddingTop: '1.5rem', borderTop: '1px solid #eee' }}>
              <p style={{ fontSize: '0.85rem', fontWeight: 600, color: '#666', marginBottom: '0.75rem' }}>
                💡 Try these examples:
              </p>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
                {exampleQuestions.map((example, index) => (
                  <button
                    key={index}
                    onClick={() => handleExampleClick(example)}
                    style={{
                      fontSize: '0.85rem',
                      padding: '0.5rem 1rem',
                      background: '#f8f9fa',
                      color: '#3498db',
                      border: '1px solid #dee2e6',
                      borderRadius: '20px',
                      cursor: 'pointer',
                      transition: 'all 0.2s'
                    }}
                    onMouseOver={(e) => {
                      e.target.style.background = '#e3f2fd';
                      e.target.style.borderColor = '#3498db';
                    }}
                    onMouseOut={(e) => {
                      e.target.style.background = '#f8f9fa';
                      e.target.style.borderColor = '#dee2e6';
                    }}
                  >
                    {example}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Error Message */}
          {error && (
            <div style={{
              background: '#f8d7da',
              border: '1px solid #f5c6cb',
              padding: '1rem 1.5rem',
              borderRadius: '6px',
              color: '#721c24',
              marginBottom: '1.5rem'
            }}>
              ⚠️ {error}
            </div>
          )}

          {/* Results */}
          {result && (
            <div style={{
              background: 'white',
              borderRadius: '8px',
              boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
              overflow: 'hidden'
            }}>
              {result.success ? (
                <>
                  {/* Results Header */}
                  <div style={{
                    padding: '1.25rem 1.5rem',
                    background: '#f8f9fa',
                    borderBottom: '1px solid #dee2e6',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center'
                  }}>
                    <h2 style={{ margin: 0, fontSize: '1.1rem', color: '#2c3e50', fontWeight: 600 }}>
                      Results ({result.resultCount} {result.resultCount === 1 ? 'row' : 'rows'})
                    </h2>
                    <span style={{ fontSize: '0.85rem', color: '#666' }}>
                      ⏱️ {result.executionTimeMs}ms
                    </span>
                  </div>

                  {/* Results Table */}
                  {result.results && result.results.length > 0 ? (
                    <div style={{ overflowX: 'auto' }}>
                      <table className="team-table" style={{ margin: 0 }}>
                        <thead>
                          <tr>
                            {Object.keys(result.results[0]).map((key) => (
                              <th key={key}>{key}</th>
                            ))}
                          </tr>
                        </thead>
                        <tbody>
                          {result.results.map((row, index) => (
                            <tr key={index}>
                              {Object.values(row).map((value, cellIndex) => (
                                <td key={cellIndex}>
                                  {value !== null && value !== undefined ? String(value) : '-'}
                                </td>
                              ))}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <div className="empty-cell">
                      No results found
                    </div>
                  )}

                  {/* SQL Display */}
                  {result.generatedSql && (
                    <details style={{
                      padding: '1rem 1.5rem',
                      background: '#f8f9fa',
                      borderTop: '1px solid #dee2e6',
                      cursor: 'pointer'
                    }}>
                      <summary style={{
                        fontSize: '0.9rem',
                        fontWeight: 500,
                        color: '#666',
                        userSelect: 'none'
                      }}>
                        📝 View Generated SQL
                      </summary>
                      <pre style={{
                        marginTop: '1rem',
                        padding: '1rem',
                        background: '#2c3e50',
                        color: '#27ae60',
                        borderRadius: '4px',
                        fontSize: '0.85rem',
                        overflowX: 'auto',
                        lineHeight: '1.5'
                      }}>
                        {result.generatedSql}
                      </pre>
                    </details>
                  )}
                </>
              ) : (
                /* Error Result */
                <div style={{ padding: '2rem' }}>
                  <div style={{ display: 'flex', gap: '1rem', alignItems: 'flex-start' }}>
                    <svg style={{ width: '24px', height: '24px', color: '#e74c3c', flexShrink: 0 }} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <div>
                      <h3 style={{ fontWeight: 600, color: '#2c3e50', marginBottom: '0.5rem' }}>
                        Unable to process query
                      </h3>
                      <p style={{ color: '#666', margin: 0 }}>{result.error}</p>
                      {result.errorCode && (
                        <p style={{ fontSize: '0.85rem', color: '#999', marginTop: '0.5rem' }}>
                          Error Code: {result.errorCode}
                        </p>
                      )}
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Help Text */}
          {!result && !loading && (
            <div style={{
              background: 'white',
              padding: '2rem',
              borderRadius: '8px',
              boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
              marginTop: '1.5rem'
            }}>
              <h3 style={{ fontSize: '1.1rem', fontWeight: 600, color: '#2c3e50', marginBottom: '1rem' }}>
                What can you ask?
              </h3>
              <ul style={{
                listStyle: 'none',
                padding: 0,
                margin: 0,
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
                gap: '0.75rem'
              }}>
                <li style={{ padding: '0.75rem', background: '#f8f9fa', borderRadius: '6px', fontSize: '0.9rem', color: '#666' }}>
                  ✓ Find active squads and their capacity
                </li>
                <li style={{ padding: '0.75rem', background: '#f8f9fa', borderRadius: '6px', fontSize: '0.9rem', color: '#666' }}>
                  ✓ Search for projects by date or customer
                </li>
                <li style={{ padding: '0.75rem', background: '#f8f9fa', borderRadius: '6px', fontSize: '0.9rem', color: '#666' }}>
                  ✓ View team members and their roles
                </li>
                <li style={{ padding: '0.75rem', background: '#f8f9fa', borderRadius: '6px', fontSize: '0.9rem', color: '#666' }}>
                  ✓ Check squad utilization
                </li>
                <li style={{ padding: '0.75rem', background: '#f8f9fa', borderRadius: '6px', fontSize: '0.9rem', color: '#666' }}>
                  ✓ Query project allocations
                </li>
                <li style={{ padding: '0.75rem', background: '#f8f9fa', borderRadius: '6px', fontSize: '0.9rem', color: '#666' }}>
                  ✓ Analyze scheduling patterns
                </li>
              </ul>
              <p style={{
                marginTop: '1.5rem',
                padding: '1rem',
                background: '#fff3cd',
                borderRadius: '6px',
                fontSize: '0.85rem',
                color: '#856404',
                margin: '1.5rem 0 0 0',
                borderLeft: '4px solid #ffc107'
              }}>
                ℹ️ <strong>Note:</strong> This feature can only read data, not modify it. All queries are validated for safety.
              </p>
            </div>
          )}
        </div>
      </div>

      <style>{`
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  );
}

export default AiQueryPage;
