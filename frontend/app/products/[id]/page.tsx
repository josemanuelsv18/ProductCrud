import { ProductDetailView } from '@/components/ProductDetailView';

type PageProps = {
  params: Promise<{ id: string }>;
};

export default async function ProductDetailPage({ params }: PageProps) {
  const { id } = await params;

  return <ProductDetailView productId={id} />;
}
