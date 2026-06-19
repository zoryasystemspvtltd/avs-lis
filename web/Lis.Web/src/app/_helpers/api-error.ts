/** Extract user-visible API error text (handles ErrorInterceptor body-only throws). */
export function extractApiError(err: any, fallback = 'Operation failed'): string {
  if (!err) {
    return fallback;
  }
  if (typeof err === 'string') {
    const text = err.trim();
    return text || fallback;
  }

  const body = err.error !== undefined && err.status != null ? err.error : err;
  if (typeof body === 'string') {
    const text = body.trim();
    return text || fallback;
  }

  if (body && typeof body === 'object') {
    const responseException = body.responseException || body.ResponseException;
    const nested =
      responseException?.exceptionMessage ||
      responseException?.ExceptionMessage ||
      responseException?.message ||
      responseException?.Message;

    return (
      body.message ||
      body.Message ||
      nested ||
      body.error ||
      body.Error ||
      (typeof body.result === 'string' ? body.result : null) ||
      (typeof body.Result === 'string' ? body.Result : null) ||
      fallback
    );
  }

  return err.message || fallback;
}
